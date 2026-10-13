# Arquitectura general

Mapa de subsistemas, flujos de datos y eventos. Describe el comportamiento real verificado en código; los detalles por subsistema están en los documentos enlazados.

## Vista de subsistemas

```text
Captura Leap (LeapDataProvider)
  → Snapshot (HandSnapshotBuilder → HandDataSnapshot)
  → MotionPipelineRunner
      ├─ MotionAggregator L/R + detectores → FrameMotionData → MotionEventBus
      │    ├─ MetricsTrackingSystem → ExerciseMetricsTracker L/R → HandUsageSummary
      │    ├─ InteractionTracker → InteractionManager → Interactable
      │    ├─ ExerciseFeedbackSystem (baseline, polling)
      │    └─ HybridSuggestionTrackingSystem (vía UsageInterpreter + HybridFramePairer)
      └─ ErgonomicAggregator L/R + detectores → FrameErgonomicData → ErgonomicEventBus
           └─ ErgonomicExposureTrackingSystem → FrameErgonomicExposureData → ErgonomicExposureEventBus
                ├─ SessionRecorder (persistencia de exposición)
                └─ HybridSuggestionTrackingSystem (empareja con uso motor)
```

Capas de juego y datos:

- **Gameplay** (`Assets/Scripts/Gameplay/`): `GameManager`, `ExerciseController`, `ExerciseProgressManager`, ejercicios Insert/OSU/Hunter, orientación fases 1–3, preparación. Ver `docs/gameplay.md`.
- **Interacción** (`Assets/Scripts/Interaction/`): traduce `FrameMotionData` a agarrar/rotar/seleccionar. Solo Insert y fase 3 usan este camino.
- **Métricas** (`Assets/Scripts/TrackingPipeline/Metrics/`): acumulado articular por mano. Ver `docs/tracking-metrics.md`.
- **Sugerencias** (`Assets/Scripts/SuggestionSystem/` + `Assets/Scripts/TrackingPipeline/ErgonomicDetection/Suggestions/`): baseline + híbrido. Ver `docs/suggestions.md`.
- **Puntuación y persistencia** (`Assets/Scripts/ScoreSystem/`, `Assets/Scripts/Persistence/`, `Assets/Scripts/Gameplay/SummaryData/`): scores 0–100, JSON en `persistentDataPath/app/`, journal transaccional de 3 ejercicios. Ver `docs/scores-persistence.md`.
- **UI/Audio/Conexión** (`Assets/Scripts/UI/`, `Assets/Scripts/Audio/`, `Assets/Scripts/Connection/`, `Assets/Scripts/Utils/`): resultados, snackbars, countdown, trofeos, sonidos, estado Leap. Ver `docs/ui-systems.md`.

Principios (ver `AGENTS.md`): separación de responsabilidades, comunicación event-driven con eventos estáticos `On*` nulo-seguros, suscripción simétrica (`+=` en `OnEnable`, `-=` en `OnDisable`), lógica pura fuera de `MonoBehaviour`, datos planos propios en lugar de tipos del SDK.

## Ciclo de vida de un ejercicio

Estados `GAMESTATE` en `Assets/Scripts/Gameplay/GameManager.cs`: `IDLE → COUNTDOWN → PLAYING → RESULTS`.

1. `DemoManager.OnDemoClosed` → `StartCountdown` → `SetState(COUNTDOWN)` → `OnCountdownStart` → `CountdownUI` cuenta y emite `OnCountdownFinished`.
2. `OnCountdownFinished` → `SetState(PLAYING)` → `OnExcerciseStart` (nótese el typo con `c`, nombre real del evento). Suscriptores verificados:
   - `MetricsTrackingSystem.RunTracking` (reset + `isTracking = true`).
   - `ErgonomicExposureTrackingSystem.RunTracking`.
   - `SessionRecorder.BeginExercise` (limpia buffers y `LastGeneralSuggestion`).
   - `HybridSuggestionTrackingSystem.BeginExercise`.
   - `ExerciseController.HandleStartExercise` → corutina `ExerciseRoutine`.
3. Durante `PLAYING`, `ExerciseController.ExerciseRoutine` (`Assets/Scripts/Gameplay/ExerciseController.cs`):
   - `SessionManager.BeginSession` si no hay sesión.
   - `OnExerciseStart` del ejercicio (adapters + runners).
   - Bucle hasta `IsExerciseCompleted` con `feedbackSystem.Evaluate(elapsed, dt)` cada frame.
   - `WaitForSeconds(exerciseEndDelay)` (3 s por defecto) y `OnExerciseEnd(duration)`.
4. `OnExerciseEnd(duration)` del controlador: `SetSpecificData()` (puntúa y guarda desempeño en `SessionRecorder`) → `SetPendingScore` → `gameManager.EndExercise(duration)`.
5. `GameManager.EndExercise` emite en orden estricto: `OnExerciseEnd(duration)` → `OnExerciseFinalizing(duration)` → `SetState(RESULTS)` → `OnShowResults`.

Orden crítico al cerrar:

- `OnExerciseEnd`: `MetricsTrackingSystem.StopTracking` y `ErgonomicExposureTrackingSystem.StopTracking` publican `OnTrackingStop(duration, left, right)`.
- `SessionRecorder` ya capturó uso y exposición vía `HybridExerciseResultSynchronizer` y en `OnExerciseFinalizing` construye `ExerciseSummary` + sugerencia final + `ExpositionSummary` y confirma con `SessionManager`.
- `ResultsManager` tenía el resumen pendiente y en `OnShowResults` pinta `ResultsUI`; `ScoresUI` ya se rellenó vía `ScoreEventBus`; `TrophyView` muestra el trofeo.

## Mapa de eventos

| Publicador | Evento | Suscriptores verificados |
|---|---|---|
| `LeapDataProvider` | `OnFrameReady(Leap.Frame)` | `MotionPipelineRunner` |
| `MotionEventBus` | `OnFrame(FrameMotionData)` | `MetricsTrackingSystem`, `HybridSuggestionTrackingSystem`, `MotionDebugLogger`, `InteractionTracker` |
| `ErgonomicEventBus` | `OnFrame(FrameErgonomicData)` | `ErgonomicExposureTrackingSystem` |
| `ErgonomicExposureEventBus` | `OnFrame(FrameErgonomicExposureData)` | `HybridSuggestionTrackingSystem` |
| `ErgonomicExposureEventBus` | `OnTrackingStop(duration, L, R)` | `SessionRecorder.results.CaptureExposure` |
| `MetricsTrackingSystem` | `OnTrackingStop(duration, L, R)` | `SessionRecorder.results.CaptureUsage`, `ResultsManager` |
| `GameManager` | `OnCountdownStart` | `CountdownUI` |
| `GameManager` | `OnExcerciseStart` | Ver lista del paso 2 |
| `GameManager` | `OnExerciseEnd(float)` | `MetricsTrackingSystem`, `ErgonomicExposureTrackingSystem`, `HybridSuggestionTrackingSystem` |
| `GameManager` | `OnExerciseFinalizing(float)` | `SessionRecorder.SaveExerciseSummary` |
| `GameManager` | `OnShowResults` | `ResultsManager`, `ScoresUI`, `TrophyView` |
| `ExerciseProgressManager` | `OnProgressChanged`, `OnPhaseChanged`, `OnPhaseCompleted`, `OnStepCompleted`, etc. | Runners, `ProgressionUI`, feedbacks de audio/vignette |
| `ScoreEventBus` | `OnScoreCompleted(ExerciseScore)` | `ScoresUI` |
| `SnackbarManager` | `OnShow(type, msg, time)` | `SnackbarUI` (no-op si no hay instancia) |
| `HybridSuggestionEventBus` | `OnSuggestion(HybridSuggestionData)` | Sin suscriptor interno (punto de extensión) |

`ExerciseFeedbackSystem` (baseline) no publica bus: hace polling desde `ExerciseController` y sale por `Debug.Log` + `SnackbarManager`.

## Dos caminos de tracking (decisión clave)

No toda la lógica usa el pipeline desacoplado:

1. **Pipeline desacoplado** (`FrameMotionData` + `MotionEventBus`): Insert (`PieceBehaviour`/`SlotBehaviour` vía `InteractionManager`) y fase 3 de orientación. Es el único camino que alimenta métricas articulares y sugerencias.
2. **Lectura raw directa**: OSU (`TargetDetector` vía `GameplayHandInput` → `LeapDataProvider.GetCurrentFrame`) y Hunter (`HandLaserPointer` vía `GameplayHandInput` + `Raycast`), orientación fase 1 y preparación (leen `LeapServiceProvider.CurrentFrame`). No pasan por detectores ni generan `MotionData`.

Consecuencia: OSU/Hunter miden desempeño (tiempos, aciertos) pero su uso articular solo se registra si hay frames en `MotionEventBus` durante `PLAYING`; la interacción no depende de `InteractionTracker`.

## Datos que cruzan subsistemas

- `HandDataSnapshot` (struct): posiciones/rotaciones de palma-muñeca-codo, direcciones, `grabStrength`/`pinchStrength`, `frameId`, `timestamp`, `HandType`. Ningún tipo `Leap.*` sale de `LeapInput/`.
- `FrameMotionData`: `frameId`, `handType`, `handPos`/`handRotation`, `timestamp`, `List<MotionData>`, `List<GestureStateData>`.
- `MotionData`: `zone` (`Hand`/`Wrist`/`Forearm`), `value` 0–1, `isActive`, velocidad/ángulo raw.
- `FrameErgonomicData` / `FrameErgonomicExposureData`: ángulos de muñeca (flexión/extensión, desviación radial/cubital, pronosupinación) + exposición acumulada/sostenida por dimensión.
- `HandUsageSummary`: `absoluteUsage`, `relativeUsage`, `intensity`, `totalActiveSeconds`, `activityRatio` por mano (solo al detener tracking).
- `ExerciseSummary`: une uso L/R + desempeño del ejercicio + `generalSuggestion`. Se persiste en `sessions.json`.
- `ExerciseScore`: `totalScore` 0–100, `trophyTier`, `breakdown`, `statsData`. Se persiste como `ScoreRecord`.
- `ExpositionSummary`: exposición L/R por dimensión. Se persiste en `expositions.json`.

## Decisiones y límites

- **Métricas runtime vs finales no intercambiables**: sugerencias en vivo usan ventana/proporciones en vivo; resultados usan `HandUsageSummary` construido al parar.
- **Ambas manos siempre**: ningún ejercicio es unilateral; la mano inactiva puede compensar y se evalúa igual, con prioridad a la más activa.
- **Feedback acotado**: máximo 3 sugerencias por ejercicio + cooldowns + deduplicación (baseline e híbrido con parámetros distintos).
- **Sesión de 3 ejercicios**: la persistencia solo confirma cuando hay Insert + OSU + DuckHunter con el mismo `sessionGuid` (journal `exercise-commit.json`).
- **Sin backend ni secretos**: todo es local JSON; leaderboards son ficheros globales, no servidor.
- **Postura ≠ diagnóstico**: umbrales ergonómicos provisionales (60 s por defecto en DuckHunter, 3 s en perfiles Default/Insert/OSU según assets) y giro del SDK sin validación anatómica; no es RULA clínico.
