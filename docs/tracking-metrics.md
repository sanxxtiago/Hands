# Tracking, métricas y exposición ergonómica

Captura Leap, pipeline de movimiento y acumulación articular. Para sugerencias ver `docs/suggestions.md` y el detalle híbrido en `Assets/Scripts/TrackingPipeline/ErgonomicDetection/Suggestions/README.md`.

## Captura (`Assets/Scripts/TrackingPipeline/LeapInput/`)

- `LeapDataProvider.cs` (`MonoBehaviour`): único contacto con Ultraleap. En `Update` lee `_provider.CurrentFrame` y publica `OnFrameReady(Leap.Frame)`. Expone `GetCurrentFrame()` para lecturas raw (OSU/Hunter lo usan vía `GameplayHandInput`).
- `HandSnapshotBuilder.cs` (POCO): convierte `Leap.Frame` en `HandDataSnapshot?` por mano (izquierda/derecha). Copia `palm/wrist/elbowPosition`, `palmNormal`, `handDirection`, `forearmDirection`, `palmRotation`, `forearmRotation`, `grabStrength`, `pinchStrength`, más `frameId`, `timestamp` y `HandType`.
- `HandDataSnapshot.cs` (struct): DTO desacoplado. A partir de aquí ningún tipo `Leap.*` circula.

## Pipeline de movimiento (`MotionPipelineRunner`, `MotionDetection/`, `EventPipeline/`)

Flujo: `LeapDataProvider → HandSnapshotBuilder → MotionAggregator (L/R) → detectores → FrameMotionData → MotionEventDispatcher → MotionEventBus`.

- `Assets/Scripts/TrackingPipeline/MotionPipelineRunner.cs`: crea en `Awake` 2 `MotionAggregator` + 2 `ErgonomicAggregator` + dispatchers; en `OnEnable/OnDisable` se suscribe a `OnFrameReady`. Por snapshot, delega a agregadores por `HandType` y exige sincronía movimiento↔ergonomía (`frameId`/`handType`/`timestamp`); si no, `LogError` y descarta.
- `MotionDetection/MotionAggregator.cs`: filtra por mano, ignora `frameId` repetido, descarta el primer frame (guarda `_previousSnapshot` y retorna). Invoca `IMotionDetector.Evaluate(current, previous)` y `IGestureDetector.Evaluate(current)` y construye `FrameMotionData`.
- Detectores continuos (`IMotionDetector.Evaluate → MotionData{zone, value 0–1, isActive, ...}`):
  - `WristRotationDetector`: ángulo entre palmas relativas al antebrazo; ruido < 0.5°, suavizado 0.2, umbral 2°, `value = clamp01(suavizado/30)`, zona `Wrist`.
  - `ForearmRotationDetector`: rotación del antebrazo; umbral 3°, `value = /30`, zona `Forearm`.
  - `HandPositionDetector`: desplazamiento de palma; ruido < 0.002 m, umbral 0.02, `value = clamp01(suavizado/0.1)` + velocidad, zona `Hand`.
- Detectores discretos (`IGestureDetector → GestureStateData{type, strength, isActive}`):
  - `GrabGestureDetector`: histéresis 0.65/0.60 sobre `grabStrength`; exige HOLD (`isActive` actual + previo).
  - `PinchGestureDetector`: si `grab > 0.7`, el pinch efectivo se reduce (×0.3); histéresis 0.65/0.55.
- `MotionDetection/FrameMotionData.cs`, `MotionData.cs`, `GestureStateData.cs`, `GestureEvent.cs` (este último definido pero sin uso en el pipeline actual).
- `EventPipeline/MotionEventBus.cs`: bus estático `OnFrame(FrameMotionData)` + `Publish` nulo-seguro. `MotionEventDispatcher.cs` es un wrapper fino.
- `MotionDebugLogger.cs`: solo debug; loguea motions activos + gestos.

Tipos compartidos en `Assets/Scripts/Utils/Enums.cs`: `HandType{LEFT,RIGHT,NONE}`, `MotionZone{Hand,Wrist,Forearm,...}`, `GestureType`, `ExerciseType{Insert,OSU,DuckHunter}`.

## Métricas (`Assets/Scripts/TrackingPipeline/Metrics/`)

Flujo: `MotionEventBus → MetricsTrackingSystem → ExerciseMetricsTracker (L/R) → MetricsSummaryBuilder → HandUsageSummary`.

- `MetricsTrackingSystem.cs`: suscrito a `MotionEventBus.OnFrame` + `GameManager.OnExcerciseStart → RunTracking` (reset + `isTracking = true`) + `OnExerciseEnd → StopTracking(duration)` (publica `OnTrackingStop(duration, left, right)`). `OnFrameReceived` filtra por `handType` y solo procesa si `isTracking`.
- `ExerciseMetricsTracker.cs` (POCO por mano): acumulado total + ventana deslizante 5 s (`Queue<WindowSample>` con bitmask por zona). Por frame calcula `dt`, toma el `value` máximo por zona, suma `strength` de gestos a `Hand`, y acumula `activeFrames/activeTime/accumulatedValue` en `ZoneUsageRecord`.
  - `GetRuntimeSnapshot()`: `activeFrames/totalFrames` por zona (acumulado; lo pinta `GameManager.Update` en `ArmRuntimeUI`).
  - `GetWindowedSnapshot()` + `GetWindowedActivityRatio()`: solo ventana (lo usa el baseline de sugerencias).
  - `GetActivityRatio(duration)`: `activeTime/elapsed`.
- `RuntimeMetrics.cs` / `ZoneUsageRecord.cs`: DTOs intermedios.
- `HandUsageSummary.cs`: resultado final por mano (`zones`, `absoluteUsage`, `relativeUsage`, `intensity`, `totalActiveSeconds`, `activityRatio`).
- `MetricsSummaryBuilder.cs` (estático): `absolute = activeFrames/totalFrames`, `relative = zoneActiveTime/suma`, `intensity = accumulatedValue/totalFrames`. **Solo se construye al parar**; alimenta `ResultsUI` y la persistencia.

No confundir runtime (proporciones en vivo) con final (absoluto/relativo/intensidad). Las sugerencias en vivo usan runtime; las gráficas de resultados usan `HandUsageSummary`.

## Postura y exposición (`Assets/Scripts/TrackingPipeline/ErgonomicDetection/`)

Dos etapas: postura frame a frame y exposición acumulada/sostenida.

Postura (`ErgonomicAggregator`, `IErgonomicDetector`, `WristPostureDetector`, `ForearmPostureDetector`, `FrameErgonomicData`, `ErgonomicEventBus/Dispatcher`):

- Gemelo del agregador de movimiento: filtra mano, anti-duplicado, descarta primer frame.
- `ForearmPostureDetector` rellena codo/muñeca, eje y rotación del antebrazo (con fallback `wrist-elbow`) y flags `Available/Valid`.
- `WristPostureDetector` exige antebrazo válido y calcula tres ángulos por proyección: flexión/extensión, desviación radial/cubital y pronosupinación (palma relativa al antebrazo). Devuelve `ErgonomicAngleData{degrees, isAvailable, isValid}`.
- Publica `ErgonomicEventBus.OnFrame(FrameErgonomicData)`.

Exposición (`Exposure/`):

- `ErgonomicExposureTrackingSystem.cs`: `calibrationProfile` en Inspector; crea 2 `ErgonomicExposureInterpreter`; escucha `ErgonomicEventBus.OnFrame` + `GameManager.OnExcerciseStart/OnExerciseEnd`. `RunTracking` resetea y activa; por frame delega por mano y si `TryProcess` publica `ErgonomicExposureEventBus.OnFrame`; `StopTracking` publica `OnTrackingStop(duration, L, R)`.
- `ErgonomicExposureInterpreter.cs` (por mano): exige `0 < dt ≤ MaximumFrameGapSeconds` (si no, resetea sostenido). Por dimensión: si deshabilitada/inválida/sin delta o dentro de rango óptimo, resetea sostenido; si fuera, acumula `cumulative += dt`, `sustained += dt`, `maxSustained`, y marca `hasReached/crossed*`.
- `ErgonomicCalibrationProfile.cs` (`ScriptableObject`, `Ergonomic Detection/Calibration Profile`): versión, `cumulativeExposureAlertSeconds`, `sustainedExposureThresholdSeconds`, `maximumFrameGapSeconds`, lista de 3 `ErgonomicAngleCalibration`. `TryValidate` exige cubrir las 3 dimensiones sin duplicados.
- `ErgonomicAngleCalibration.cs`: `dimension`, `isEnabled`, `min/maxOptimalDegrees`, nota. `ErgonomicPostureDimension.cs`: flexión/extensión, desviación radial/cubital, pronosupinación.
- `FrameErgonomicExposureData` + `ErgonomicExposureDimensionData`: grados, disponibilidad/validez, `isOutsideOptimalRange`, segundos acumulados/sostenidos y flancos.
- Resúmenes `HandErgonomicExposureSummary` / `ErgonomicExposureDimensionSummary`: `validObservationSeconds`, `maximumSustainedExposureSeconds` (episodio continuo más largo; distinto de `sustainedExposureSeconds` actual), `cumulativeExposureSeconds`, flags. Incluyen `calibrationProfileId` para validar compatibilidad al cerrar.
- `ErgonomicExposureEventBus.cs`: `OnFrame` + `OnTrackingStop`.

Calibraciones en `Assets/Resources/ErgonomicProfiles/`: `Default/Insert/OSU` (3 s/3 s aprox.) y `DuckHunter` (60 s/60 s). Los rangos óptimos son provisionales del prototipo, no RULA clínico; el giro palma–antebrazo del SDK no está validado anatómicamente.

## Configuración relevante

- Baseline: `Assets/Scripts/SuggestionSystem/Profiles/*ExerciseProfile.asset` (targets 0–100 normalizados en `OnValidate`, tolerancias, criticidades).
- Híbrido/ergonomía: `Assets/Resources/ErgonomicProfiles/*CalibrationProfile`, `*HybridRuntimeProfile`, `*HybridExerciseProfile`, `DefaultHybridSuggestionProfile` (ventana 5 s, observación mínima 2 s, señal mínima 0.01, bandas 0.35/0.65, hold 2 s, warmup 2 s, cooldown 8 s salvo Default 15 s, máximo 3).
- Contenido de ejercicios (`Assets/Resources/OSUSequences/`, `DuckSequences/`, `OSUPaths/`) no son umbrales: son fases, pasos, manos y curvas Bézier.
