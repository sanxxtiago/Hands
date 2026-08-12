# Hands Project Context & Guidelines

## Overview
- **Project Name:** Hands
- **Engine / Tech Stack:** Unity 2023.1.22f1 (URP 15.0.7)
- **Primary SDKs:**
  - Ultraleap Tracking SDK (`com.ultraleap.tracking` v7.3.0)
  - OpenXR / Oculus XR Integration
  - DOTween & Unity UI Extensions

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

- **Compilar en CLI:** `dotnet build "Hands.sln" --no-restore` desde la raíz del proyecto. `Assembly-CSharp.csproj` contiene los scripts del juego.
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