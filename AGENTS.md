# Hands Project Context & Guidelines

## Overview
- **Project Name:** Hands
- **Engine / Tech Stack:** Unity 2023.1.22f1 (URP 15.0.7)
- **Primary SDKs:**
  - Ultraleap Tracking SDK (`com.ultraleap.tracking` v7.3.0)
  - OpenXR / Oculus XR Integration
  - DOTween & Unity UI Extensions

---

## Objetivo del Prototipo

**Hands** es un prototipo de prevención motora del miembro superior basado en **seguimiento de manos sin contacto** (Ultraleap Leap Motion / OpenXR). Su objetivo es que el paciente realice **ejercicios de terapia guiados** mientras el sistema **observa y cuantifica el uso articular** (mano, muñeca, antebrazo) en tiempo real, sin depender de sensores o dispositivos que el usuario deba sujetar.

El prototipo persigue tres metas:
1. **Ejercitar** el miembro superior mediante actividades gamificadas que motivan a mover mano, muñeca y antebrazo.
2. **Medir** el patrón de movimiento (zonas articulares usadas, tiempo activo, intensidad) para cada mano y por ejercicio.
3. **Guiar** al paciente con feedback inmediato (sugerencias en vivo) y una sugerencia final por ejercicio y por sesión.

## Funcionamiento General

### Ciclo de una sesión
1. **Orientación del usuario** (`UserOrientation`): el paciente aprende a posicionar las manos frente al sensor antes de empezar (fases previas al ejercicio, ej. `Phase3`).
2. **Cuenta atrás** (`CountdownUI`): tras iniciar el ejercicio, una cuenta regresiva da tiempo a prepararse (p. ej. permite configurar la quirralidad de las piezas).
3. **Ejercicio activo** (`GAMESTATE.PLAYING`): el paciente interactúa mientras el sistema acumula métricas y emite sugerencias en vivo.
4. **Resultados** (`GAMESTATE.RESULTS`): se muestran las métricas por mano (uso de zonas absoluto/relativo) junto con la sugerencia general del ejercicio.
5. **Resumen de sesión** (`SessionSummary`): el terapeuta/paciente revisa el acumulado de todos los ejercicios realizados.

### Pila de datos (cómo funciona por dentro)
```
Captura (LeapDataProvider)                    → frames raw de Ultraleap
  ➔ Snapshot (HandSnapshotBuilder)            → datos desacoplados por mano (posiciones, rotaciones, gestos)
  ➔ Pipeline de movimiento (MotionPipelineRunner)
      • Agregadores (MotionAggregator L/R)    → valida, filtra por mano, evita frames duplicados
      • Detectores continuos/discretos        → rotación muñeca, antebrazo, desplazamiento palma, grab/pinch
  ➔ FrameMotionData → MotionEventBus          → bus global (un único frame por frameId por mano)
      ├─ MetricsTrackingSystem                → acumula uso articular (zona, frames activos, tiempo)
      │    ➔ ExerciseMetricsTracker (L/R) ➔ MetricsProcessor ➔ HandUsageSummary (resultados)
      └─ InteractionTracking (InteractionTracker + InteractionManager)
           ➔ Gestos (GRAB/PINCH/ROTATE) ➔ Interactable (agarrar/soltar/rotar/seleccionar)
      └─ SuggestionSystem (ExerciseFeedbackSystem)
           ➔ Engines por mano + reglas (exceso mano/muñeca, baja participación antebrazo, baja actividad)
           ➔ Selección anti-spam ➔ Debug.Log + Snackbar (feedback en vivo)
```

### Experiencia de usuario (qué percibe el paciente)
- **Insert:** coge piezas con la mano indicada y las encaja en el slot de su pared. Primera aproximación a la terapia manipulativa (agarre + precisión + rotación).
- **OSU:** alcanza objetivos que aparecen en pantalla con la mano correcta; mide la velocidad de reacción y la interacción acumulada.
- **DuckHunter:** apunta y "dispara" a patos con el láser de la mano; trabaja precisión, mantenimiento de postura y seguimiento visual.
- **Feedback en vivo:** si compensa con la mano en lugar de mover el antebrazo (o viceversa), o si mantiene baja actividad, recibe una advertencia inmediata (snackbar + consola), con un máximo de 3 por ejercicio para no saturar.
- **Feedback final:** al terminar, una frase resume si resolvió el ejercicio en un tiempo adecuado o con pocos fallos.

### Decisiones de diseño clave
- **Desacoplamiento total del tracking:** ninguna lógica de juego conoce el SDK de Leap; todo pasa por `FrameMotionData` y `MotionEventBus`.
- **Dos métricas distintas y no intercambiables:** las **sugerencias en vivo** usan métricas relativas de runtime (`activeFrames / totalFrames`); los **resultados finales** usan `HandUsageSummary` (absoluto, relativo, intensidad) construido solo al detener el tracking.
- **Feedback acotado:** anti-spam estricto (máx. 3 sugerencias, cooldown, deduplicación) para que la terapia no se convierta en ruido.
- **Evaluación de ambas manos siempre:** no se descarta la mano "inactiva" porque puede estar compensando; la prioridad la marca la mano con más movimiento.

---

## Captura y Arquitectura de Frames (Leap Motion)

1. **Captura de Frames Raw (`LeapDataProvider`)**
   - Actúa como puente con el SDK de Ultraleap, obteniendo `_provider.CurrentFrame` en cada `Update()`.
   - Emite el evento `OnFrameReady(Leap.Frame)` cuando hay un frame de tracking disponible.

2. **Extracción y Snapshot (`HandSnapshotBuilder`)**
   - Transforma el objeto nativo `Leap.Frame` en estructuras desacopladas de datos (`HandDataSnapshot`).
   - Extrae por separado datos de la mano izquierda y derecha:
     - **Posiciones:** `palmPosition`, `wristPosition`, `elbowPosition`.
     - **Direcciones y Orientaciones:** `palmNormal`, `handDirection`, `forearmDirection`, `palmRotation`, `forearmRotation`.
     - **Fuerza de Gestos:** `grabStrength`, `pinchStrength`.

3. **Conducción del Pipeline (`MotionPipelineRunner`)**
   - Suscrito a `LeapDataProvider.OnFrameReady`.
   - Instancia y coordina `HandSnapshotBuilder`, dos agregadores de movimiento (`MotionAggregator` para mano izquierda y derecha) y el despachador de eventos `MotionEventDispatcher`.

---

## Core 1: Tracking Pipeline

El **Tracking Pipeline** procesa el flujo de movimiento cuadro a cuadro y desacopla la detección física de la lógica del juego.

### Flujo de Datos
`LeapDataProvider` ➔ `HandSnapshotBuilder` ➔ `MotionAggregator` (L/R) ➔ `Detectores (IMotionDetector / IGestureDetector)` ➔ `FrameMotionData` ➔ `MotionEventDispatcher` ➔ `MotionEventBus`

### Componentes Clave
- **`MotionAggregator`:**
  - Valida y filtra el snapshot por tipo de mano (`LEFT` / `RIGHT`).
  - Evita reprocesar el mismo `frameId`.
  - Retiene `_previousSnapshot` para calcular deltas temporales y variaciones de movimiento.
- **Detectores Continuos (`IMotionDetector`):**
  - `WristRotationDetector`: Evalúa rotación y deltas de la muñeca.
  - `ForearmRotationDetector`: Evalúa rotación y movimiento del antebrazo.
  - `HandPositionDetector`: Evalúa desplazamientos y velocidad de la palma.
  - Generan instancias de `MotionData` mapeadas a una `MotionZone` (`Hand`, `Wrist`, `Forearm`).
- **Detectores Discretos (`IGestureDetector`):**
  - `GrabGestureDetector`: Evalúa estado e intensidad del agarre.
  - `PinchGestureDetector`: Evalúa estado e intensidad del pellizco.
  - Generan instancias de `GestureStateData`.
- **`MotionEventBus`:** Bus de eventos global estático que publica objetos `FrameMotionData` procesados para cualquier consumidor en la escena.

---

## Core 2: Metrics System

El **Metrics System** registra y acumula el uso articular y motor del usuario durante un ejercicio.

### Flujo de Métricas
`MotionEventBus` ➔ `MetricsTrackingSystem` ➔ `ExerciseMetricsTracker` (L/R) ➔ `MetricsProcessor`

### Componentes Clave
- **`MetricsTrackingSystem`:**
  - Escucha `MotionEventBus.OnFrame` únicamente cuando `isTracking = true` (iniciado por `GameManager.OnExcerciseStart`).
  - Redirige las métricas de cada cuadro al tracker correspondiente (`leftTracker` / `rightTracker`).
  - Emite `OnTrackingStop` con un `HandUsageSummary` final al culminar el ejercicio.
- **`ExerciseMetricsTracker`:**
  - Acumula frames, delta tiempos (`dt`), tiempo activo total y uso por zona articular (`MotionZone`).
  - Mantiene registros `ZoneUsageRecord` almacenando `accumulatedValue`, `activeFrames` y `activeTime` para cada zona.
- **Métricas en runtime vs acumuladas (no confundir):**
  - **Runtime (`RuntimeMetrics` / `GetRuntimeSnapshot`):** uso por zona como `activeFrames / totalFrames` (proporción en vivo). Es la única fuente de las sugerencias durante el ejercicio.
  - **Final (`HandUsageSummary` / `MetricsSummaryBuilder.Build`):** `absoluteUsage`, `relativeUsage`, `intensity`. Solo se construye al detener el tracking y alimenta las gráficas de resultados (absolutas y relativas).
- **`MetricsProcessor`:**
  - `Normalize(RuntimeMetrics)`: convierte el uso por zona en proporciones relativas (`hand`, `wrist`, `forearm`).
  - `GetDeviation(NormalizedMetrics, HandProfile)`: desviación = `(normalizado - objetivo) / tolerancia`, con piso mínimo de tolerancia para evitar división por cero.

---

## Core 3: SuggestionSystem

Orienta al paciente **durante el ejercicio** (máx. 2-3 sugerencias, por defecto 3) y entrega **una sugerencia general al finalizar**, basada en una variable específica de cada ejercicio.

### Flujo en runtime
`GetRuntimeSnapshot()` ➔ `MetricsProcessor.Normalize` + `GetDeviation` ➔ `AnalysisContext` ➔ `SuggestionEngine` (por mano) ➔ `ExerciseFeedbackSystem` (selección + anti-spam) ➔ `Debug.Log` + `SnackbarManager.Show(WARNING)`

### Componentes Clave (`Assets/Scripts/SuggestionSystem/`)
- **`ExerciseFeedbackSystem`:** punto de entrada (MonoBehaviour). Mantiene **dos engines** (`leftEngine` / `rightEngine`) discriminados por `HandType`, más una regla global de baja actividad. Parametrizable en el Inspector: `warmupTime` (2s), `maxSuggestionsPerExercise` (3), `suggestionCooldown` (8s), `snackbarDuration` (3s), `zoneEvalMinActivity` (0.05).
- **`SuggestionEngine`:** ejecuta las `TimedRule` de una mano y devuelve la de mayor severidad.
- **`TimedRule`:** exige la condición sostenida (`triggerTime`) antes de disparar y aplica `cooldownTime` interno.
- **Reglas (`IRule`):**
  - `ExcessHandRule` / `ExcessWristRule`: sobreuso/compensación (`deviation > 1`).
  - `LowForearmRule`: falta de participación del antebrazo (`deviation < -1`).
  - `LowActivityRule`: baja actividad; **nivel global**, se evalúa contra la mano más activa (ver abajo).
- **`ExerciseProfile` / `HandProfile` / `ZoneTarget`:**
  - Target por zona en `editorValue` (0-100) normalizado automáticamente (`NormalizeZones`) y `tolerance`.
  - **Criticidad por zona:** `handCriticality = 1.0`, `wristCriticality = 0.8`, `forearmCriticality = 0.6` (configurable por mano).
- **`Suggestion`:** `message`, `severity` y `type` (`Zone` | `LowActivity`).

### Severidad y criticidad
- Las reglas de zona calculan severidad normalizada: `clamp01(deviation ± 1) × criticality`. Todas compiten en escala [0,1] en el `SuggestionEngine`.
- Orden de criticidad aplicado por perfil: **mano > muñeca > antebrazo** (ante igual desviación, gana la mano).

### Priorización por mano activa y ambos manos
- **Ambos perfiles de mano se evalúan siempre** (no se filtra por `isActive`; ningún ejercicio es actualmente unilateral y la mano inactiva puede estar compensando).
- Una mano con `activityRatio < zoneEvalMinActivity` no evalúa reglas de zona (su señal de desviación es ruido).
- Score por sugerencia: `severity × (0.25 + 0.75 × activityRatio)`. Se emite la sugerencia de mayor score → la mano con más movimiento tiene prioridad.

### Anti-spam estricto
1. Máximo `maxSuggestionsPerExercise` por ejercicio.
2. Cooldown global `suggestionCooldown` entre emisiones (los engines siguen evaluando durante el cooldown).
3. Deduplicación por mensaje (no se repite la misma sugerencia).
4. Cooldown interno `TimedRule` por regla.

### Sugerencia general final
- Se genera al terminar el ejercicio en `SessionRecorder.SaveExerciseSummary` (suscrito a `MetricsTrackingSystem.OnTrackingStop`).
- `GeneralSuggestionBuilder.Build(exerciseType, ...)` por ejercicio:
  - **Insert:** `completionTime` (umbrales 60s / 120s).
  - **OSU:** `TotalInteractionTime` (umbrales 30s / 60s).
  - **DuckHunter:** `missedRatio = ducksMissed / (ducksHit + ducksMissed)` (umbrales 0.2 / 0.5).
- Se guarda en `ExerciseSummary.generalSuggestion` y se expone en `SessionRecorder.LastGeneralSuggestion` (estática) para la UI.
- Consumidores de la UI:
  - `ResultsUI.generalSuggestionText`: texto en el panel de resultados de cada ejercicio (junto al uso de zonas de ambas manos), lee `LastGeneralSuggestion`.
  - `SessionReader.generalSuggestionText`: texto en la escena `SessionSummary`, lee `CurrentSummary.generalSuggestion` y se refresca al cambiar de ejercicio en el dropdown.

---

## Ejercicios

Los tres ejercicios se ejecutan como escenas independientes y heredan de `ExerciseController`. Cada controlador conecta la lógica específica del ejercicio con `ExerciseProgressManager`. Al terminar, `OnExerciseEnd` invoca `SetSpecificData()` (envía la variable específica al `SessionRecorder`) y luego `gameManager.EndExercise(duration)`.

### Insert

- **Escena:** `Assets/Scenes/Insert_Config1.unity`.
- **Controlador:** `Assets/Scripts/Gameplay/Insert/WallInsertExercise.cs` (`WallInsertExercise`).
- **Objetivo:** tomar piezas físicas y colocarlas en el slot correspondiente de una pared.
- **Variable específica:** `CompletionTime` (duración del ejercicio) → `SetInsertPiecesData`.
- **Configuración:** cada `PieceBehaviour` define un `SlotType`, una mano requerida (`HandType`) y opcionalmente si necesita rotación.
- **Interacción:** `SlotBehaviour` detecta la pieza mediante `OnTriggerStay`, valida tipo y distancia, y ejecuta una alineación interpolada hacia `snapPoint`; si corresponde, también interpola la rotación.
- **Finalización:** `PieceBehaviour.Snap()` bloquea el Rigidbody, desactiva física/colisiones y emite `OnPieceSnapped` → `progressManager.AddCompletedStep()`.

### OSU

- **Escena:** `Assets/Scenes/OSU.unity`.
- **Controlador:** `Assets/Scripts/Gameplay/OSU/OSUBasedExercise.cs` (`OSUBasedExercise`).
- **Datos:** `OSUSequence` (lista de `OSUStep`: prefab, posición, mano requerida, `PathData` opcional). Secuencias en `Assets/Resources/OSUSequences/`.
- **Objetivo:** alcanzar objetivos visuales con la mano correcta. `TargetDetector` detecta impacto por `hitRadius`.
- **Variable específica:** `TotalInteractionTime` + `InteractionCount` (acumulado de tiempo hasta tocar cada dot) → `SetOsuData`.
- **Secuencia:** `OSUSequenceRunner` instancia un objetivo a la vez; al completar/perder, avanza al siguiente.
- **Objetivos móviles:** `TrackingDotBehaviour` recorre una curva Bézier; el usuario debe mantener la mano en el radio del objetivo o falla en `0.3s`.

### DuckHunter

- **Escena:** `Assets/Scenes/DuckHunter.unity`.
- **Controlador:** `Assets/Scripts/Gameplay/Hunter/HunterExercise.cs` (`HunterExercise`).
- **Datos:** `DuckSequence` (retraso, lado, mano requerida, duración del movimiento). Secuencias en `Assets/Resources/DuckSequences/`.
- **Objetivo:** apuntar y disparar a patos con el láser de la mano (`HandLaserPointer` + poses de `HandPoseListener`).
- **Variable específica:** `DucksHit` / `DucksMissed` → `SetDuckHunterData`.
- **Movimiento:** `DuckBehaviour` interpola entre límites; si llega al destino sin ser cazado emite `OnReachedDestination` (paso fallido).

---

## Contexto adicional importante

- **Compilar en CLI:** `dotnet build "Hands.sln" --no-restore` desde la raíz del proyecto. `Assembly-CSharp.csproj` contiene los scripts del juego. Si falla con `NETSDK1004` (falta `Temp/obj/**/project.assets.json`, p. ej. tras limpiar `Temp/`) o `MSB4166` (nodo MSBuild cerrado), recompilar sin `--no-restore` y con `-m:1`.
- **Proyectos generados por Unity:** los `.csproj` se regeneran en `Temp/`. Al crear **archivos `.cs` nuevos**, el proyecto puede no incluirlos hasta que Unity reimporte/regenera. Por eso las clases nuevas se colocaron dentro de archivos ya incluidos (ej. `GeneralSuggestionBuilder` vive en `SessionRecorder.cs`).
- **Campos que exigen asignación en Inspector** (no auto-cablean):
  - `ResultsUI.generalSuggestionText` en las 3 escenas de ejercicio.
  - `SessionReader.generalSuggestionText` en `SessionSummary.unity`.
- **Snackbar:** para ver sugerencias en runtime debe existir un `SnackbarUI` activo; `SnackbarManager.OnShow` es no-op si no hay suscriptor.
- **Perfiles de ejercicio:** las 3 escenas asignan el mismo `Insert_ExerciseProfile` (guid `985222d45ab32e24c9709c8c8ac63a27`). Se recomienda crear un perfil por ejercicio ajustando targets, tolerancias y criticidad por mano.
- **`isActive` en `HandProfile`:** quedó como dato informativo; el sistema evalúa ambas manos siempre. Ningún ejercicio es actualmente unilateral.
- **Modo validación:** las sugerencias de runtime se muestran por `Debug.Log` además del snackbar.

---

## Convenciones de Código y Estándares
- **Lenguaje:** C# (.NET Standard 2.1)
- **Nomenclatura:**
  - `PascalCase` para Clases, Structs, Enums, Métodos y Propiedades públicas/serializadas (`[SerializeField] private float targetSpeed;`).
  - `camelCase` para variables privadas o locales (`private float speed;`).
  - Modificadores de acceso explícitos en todos los miembros.
- **Buenas Prácticas Unity:**
  - Priorizar `[SerializeField] private` sobre campos públicos para el Inspector.
  - Evitar asignaciones de memoria pesadas (GC Allocations) dentro de `Update()`.
  - Cancelar siempre suscripciones a eventos en `OnDestroy()` u `OnDisable()`.
- **Archivos Meta:**
  - Unity requiere un archivo `.meta` para cada asset/script creado o cambiado.

---

## Estilo de Código y Pautas de Diseño

Estas pautas destilan el estilo ya aplicado en el proyecto. Prevalecen dos principios: **separación de responsabilidades** (cada componente hace una sola cosa y la hace bien) y **comunicación event-driven** (los subsistemas no se conocen entre sí; se hablan por eventos y datos desacoplados).

### 1. Separación de responsabilidades
- **Controladores de ejercicio como orquestadores:** `WallInsertExercise`, `OSUBasedExercise` y `HunterExercise` solo traducen los eventos de su dominio a llamadas de `ExerciseProgressManager` (y `SessionRecorder` vía `SetSpecificData()`). Nunca gestionan spawns ni índices internos.
- **Runners dueños del flujo secuencial:** quien instancia pasos, avanza fases y administra corutinas es un *runner* dedicado (`OSUSequenceRunner`, `DuckSequenceRunner`) con una única entrada pública (ej. `StartSequence(sequence, controller)`).
- **Lógica pura fuera de MonoBehaviour:** los cálculos viven en clases estáticas o POCO (`MetricsProcessor`, `MetricsSummaryBuilder`, `GeneralSuggestionBuilder`) para poder razonarlos y probarlos sin escena.
- **Estrategias por interfaz:** las familias intercambiables se modelan con interfaces (`IMotionDetector`, `IGestureDetector`, `IRule`) y se componen/decoran al construirse (`TimedRule` envuelve un `IRule` añadiendo `triggerTime`/cooldown).
- **Un único punto de contacto con el SDK:** solo la capa de captura (`LeapDataProvider`) conoce tipos de Ultraleap; todo el resto del juego consume `HandDataSnapshot` / `FrameMotionData`.

### 2. Paradigma event-driven
- **Eventos estáticos tipados con prefijo `On`:** `public static event Action<int, int> OnProgressChanged;`. El publicador nunca referencia a sus suscriptores.
- **Disparo siempre nulo-seguro:** `OnEvent?.Invoke(args);`. Si un evento se dispara desde varios sitios, centralizar la invocación en métodos privados `Publish*` (ver `ExerciseProgressManager.PublishExerciseProgress`).
- **Suscripción simétrica y garantizada:** `+=` en `OnEnable` y `-=` en `OnDisable` (o `Start`/`OnDestroy`); jamás dejar suscripciones vivas. Al heredar ciclos de vida (`ExerciseController`), llamar siempre a `base.OnEnable()` / `base.OnDisable()`.
- **Handlers privados y descriptivos:** `private void OnFrameReceived(FrameMotionData frame)`; el handler extrae lo que necesita de los argumentos, sin encuestar al publicador.
- **Buses estáticos reservados a difusión global:** `MotionEventBus` (frames procesados) y `SnackbarManager` (UI global). Un bus nuevo debe justificarse; por defecto, eventos estáticos del propio subsistema.
- **Estado que emite, consumidores que reaccionan:** `GameManager.SetState` traduce cada transición a un evento (`OnCountdownStart`, `OnExcerciseStart`, `OnShowResults`); sistemas como `MetricsTrackingSystem` arrancan/paran su trabajo al recibirlos, sin que `GameManager` sepa de su existencia.

### 3. Datos y configuración
- **Datos planos cruzando límites:** estructuras serializables propias (`FrameMotionData`, `MotionData`, `GestureStateData`) en lugar de referencias a componentes o tipos de SDK.
- **Contenido en ScriptableObjects:** perfiles (`ExerciseProfile`) y secuencias con fases (`OSUSequence`, `DuckSequence`) como assets en `Assets/Resources/...`; el código no hardcodea contenido.
- **Composición declarativa en Inspector:** clases `[Serializable]` con campos `[SerializeField] private` y propiedades de solo lectura (`InsertPhaseDefinition.Prefab => prefab;`).
- **Parámetros ajustables acotados:** `[SerializeField, Min(0f)] private float x = valorPorDefecto;` y `[Tooltip("...")]` en español cuando el campo no se explica solo.

### 4. Convenciones Unity observadas
- **Corutinas administradas:** guardar el handle (`private Coroutine phaseFadeCoroutine;`), cancelar antes de relanzar (`StopXxx()` + null) y limpiar en `OnDisable`/transiciones (`StopAllCoroutines()` + resetear flags de estado). Esperas con `WaitForSeconds` / `WaitUntil`.
- **Guard clauses y salidas tempranas** en lugar de anidamiento (`if (!isTracking) return;`).
- **Propiedades expresivas con cuerpo de expresión** (`=>`) para exponer estado interno de solo lectura.
- **Cero presión de GC en caminos calientes:** arreglos cacheados, `Array.Empty<T>()`, snapshots por valor; nada de `new` recurrentes dentro de `Update()`.
- **Validación explícita de configuración:** `Debug.LogError` si falta un prefab/asset obligatorio (marcando estado inválido), `Debug.LogWarning` ante desfases recuperables (p. ej. conteo de piezas ≠ esperado). Mensajes en español con prefijo del subsistema: `"[SuggestionSystem] ..."`, `"Insert: ..."`, `"OSU: ..."`.
- **Identificadores en inglés; texto de usuario, logs y tooltips en español.**
- **Comentarios escasos**, y solo para aclarar decisiones no obvias, redactados en español.
- **`var` solo cuando el tipo es evidente** en la propia declaración (`new`, resultado inmediato); en el resto, tipo explícito.
- **Clases auxiliares anidadas y `sealed`** cuando son detalle privado del archivo (ej. `RendererFadeData` dentro de `WallInsertExercise.cs`), evitando crear `.cs` nuevos hasta que Unity regenere los `.csproj`.

### 5. Checklist rápido para código nuevo
1. ¿El componente funciona sin conocer a quién le habla? Si no, mover la comunicación a un evento.
2. ¿Toda suscripción tiene su `-=` garantizado en disable/destroy?
3. ¿La lógica pura está fuera del MonoBehaviour?
4. ¿Los datos que cruzan subsistemas son estructuras propias, no tipos del SDK?
5. ¿Los valores ajustables están en el Inspector con `Min`/`Tooltip`, no hardcodeados?
6. ¿Las corutinas y flags de estado se reinician bien ante disable/reinicio?
7. ¿Compila con `dotnet build "Hands.sln" -m:1` sin errores ni advertencias nuevas?