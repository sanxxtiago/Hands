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

## Core 2: Metrics System & Feedback Engine

El **Metrics System** registra, acumula y analiza el uso articular y motor del usuario durante un ejercicio, alimentando el motor de sugerencias.

### Flujo de Métricas
`MotionEventBus` ➔ `MetricsTrackingSystem` ➔ `ExerciseMetricsTracker` (L/R) ➔ `MetricsProcessor` ➔ `SuggestionEngine` ➔ `ExerciseFeedbackSystem`

### Componentes Clave
- **`MetricsTrackingSystem`:**
  - Escucha `MotionEventBus.OnFrame` únicamente cuando `isTracking = true` (iniciado por `GameManager.OnExcerciseStart`).
  - Redirige las métricas de cada cuadro al tracker correspondiente (`leftTracker` / `rightTracker`).
  - Emite `OnTrackingStop` con un `HandUsageSummary` final al culminar el ejercicio.
- **`ExerciseMetricsTracker`:**
  - Acumula frames, delta tiempos (`dt`), tiempo activo total y uso por zona articular (`MotionZone`).
  - Mantiene registros `ZoneUsageRecord` almacenando `accumulatedValue`, `activeFrames` y `activeTime` para cada zona.
- **`MetricsProcessor`:**
  - **Normalización:** Normaliza las métricas acumuladas (`Normalize(RuntimeMetrics)`) para obtener proporciones de uso relativo (`hand`, `wrist`, `forearm`).
  - **Cálculo de Desviación:** Compara las métricas normalizadas con los objetivos del perfil del ejercicio (`HandProfile`), calculando la desviación estándar según tolerancias (`GetDeviation`).
- **Motor de Sugerencias (`SuggestionEngine` & `ExerciseFeedbackSystem`):**
  - Evalúa continuamente las métricas en tiempo real tras un periodo de calentamiento (`warmupTime`).
  - Aplica reglas temporales (`TimedRule`) implementando `IRule`:
    - `LowActivityRule`: Detecta falta de actividad o movimiento.
    - `ExcessWristRule`: Detecta sobreuso o compensación con la muñeca.
    - `LowForearmRule`: Detecta falta de movimiento en el antebrazo.
    - `ExcessHandRule`: Detecta uso excesivo/forzado de la mano.
  - Selecciona la sugerencia de mayor severidad y emite alertas de corrección postural/motora.

---

## Ejercicios

Los tres ejercicios se ejecutan como escenas independientes y heredan de `ExerciseController`. Cada controlador conecta la lógica específica del ejercicio con `ExerciseProgressManager`, que registra pasos completados o fallidos. El ejercicio se inicia desde el flujo común de `GameManager` y sus datos de secuencia se configuran mediante assets `ScriptableObject` cuando aplica.

### Insert

- **Escena:** `Assets/Scenes/Insert_Config1.unity`.
- **Controlador:** `Assets/Scripts/Gameplay/Insert/WallInsertExercise.cs` (`WallInsertExercise`).
- **Objetivo:** tomar piezas físicas y colocarlas en el slot correspondiente de una pared.
- **Configuración:** cada `PieceBehaviour` define un `SlotType`, una mano requerida (`HandType`) y opcionalmente si necesita rotación. El controlador recibe `piecesCount` para inicializar el progreso.
- **Interacción:** las piezas son objetos físicos interactuables. `SlotBehaviour` detecta la pieza mediante `OnTriggerStay`, valida tipo y distancia, y ejecuta una alineación interpolada hacia `snapPoint`; si corresponde, también interpola la rotación.
- **Finalización:** al completar el encaje, `PieceBehaviour.Snap()` bloquea el Rigidbody, desactiva física/colisiones y emite `PieceBehaviour.OnPieceSnapped`. `WallInsertExercise` escucha ese evento y llama a `progressManager.AddCompletedStep()`.
- **Mano requerida:** después de finalizar la cuenta regresiva, `PieceBehaviour` configura `IgnorePhysicalHands` para permitir la interacción únicamente con la mano indicada.

### OSU

- **Escena:** `Assets/Scenes/OSU.unity`.
- **Controlador:** `Assets/Scripts/Gameplay/OSU/OSUBasedExercise.cs` (`OSUBasedExercise`).
- **Datos:** `OSUSequence` contiene una lista ordenada de `OSUStep`; cada paso referencia un prefab, una posición inicial, una mano requerida y opcionalmente un `PathData` de curvas Bézier. La secuencia usada está en `Assets/Resources/OSUSequences/`.
- **Objetivo:** alcanzar objetivos visuales con la mano correcta. El `TargetDetector` consulta `GameplayHandInput` y detecta el impacto cuando la posición de la mano entra en el `hitRadius` del objetivo.
- **Secuencia:** `OSUSequenceRunner` instancia un objetivo a la vez, asigna su color según `requiredHand` y lo conecta al detector. Al completar o perder un objetivo, actualiza el progreso, destruye el objeto y genera el siguiente paso.
- **Objetivos móviles:** un `TrackingDotBehaviour` puede incluir un recorrido Bézier. Tras el impacto inicial, el usuario debe mantener la mano dentro del radio del objetivo mientras este avanza por las curvas; si permanece fuera más de `0.3` segundos, el objetivo falla. Al terminar el recorrido, se registra como completado.
- **Tiempo límite:** `DotBehaviour` emite `OnMissed` si no se alcanza dentro de `timeToInteract`. El controlador registra el resultado con `AddCompletedStep()` o `AddMissedStep()`.

### DuckHunter

- **Escena:** `Assets/Scenes/DuckHunter.unity`.
- **Controlador:** `Assets/Scripts/Gameplay/Hunter/HunterExercise.cs` (`HunterExercise`).
- **Datos:** `DuckSequence` contiene pasos con retraso previo, lado de aparición, mano requerida y duración del movimiento. La secuencia se encuentra en `Assets/Resources/DuckSequences/`.
- **Objetivo:** apuntar y disparar a patos que cruzan el espacio de juego, usando la mano indicada en cada paso.
- **Secuencia:** `DuckSequenceRunner` espera `delayBeforeSpawn`, instancia un pato y espera hasta que este sea cazado o llegue a su destino. Después emite el evento correspondiente y continúa con el siguiente paso hasta emitir `OnSequenceCompleted`.
- **Movimiento del pato:** `DuckBehaviour` calcula el origen y destino a partir de los límites izquierdo y derecho, mueve el pato con `Vector3.Lerp` durante `movementDuration` y asigna su color según la mano requerida. Si llega al destino, emite `OnReachedDestination` y el paso se considera fallido.
- **Apuntado y disparo:** `HandLaserPointer` obtiene la posición y rotación de la mano desde `GameplayHandInput`, proyecta un `Raycast` y mantiene el pato detectado como objetivo. `HandPoseListener` traduce las poses de apuntar y disparar en eventos; al disparar, el pato recibe `Hit(handType)` y valida la mano requerida.
- **Progreso:** `HunterExercise` escucha `OnDuckHit` y `OnDuckMissed` para registrar pasos completados o fallidos mediante `ExerciseProgressManager`.

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
