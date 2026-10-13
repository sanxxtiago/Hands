# Sugerencias en vivo y finales

Dos sistemas coexisten: **baseline** (uso articular, polling) e **híbrido** (uso + postura, event-driven). El baseline conserva la pantalla por defecto; el híbrido procesa en `LogOnly` salvo que se active `LogAndSnackbar`. No activar ambas salidas visuales en comparativas.

Detalle híbrido normativo en `Assets/Scripts/TrackingPipeline/ErgonomicDetection/Suggestions/README.md`; aquí solo el mapa necesario para mantener y depurar.

## Baseline (`Assets/Scripts/SuggestionSystem/`)

Polling desde `ExerciseController.ExerciseRoutine`: `feedbackSystem.BeginExercise()` al inicio y `Evaluate(elapsed, dt)` cada frame hasta completar.

- `ExerciseFeedbackSystem.cs` (`MonoBehaviour`): dos `SuggestionEngine` (L/R) + regla global de baja actividad. Inspector verificado en código: `profile`, `trackingSystem`, `warmupTime` 2 s, `maxSuggestionsPerExercise` 3, **`suggestionCooldown` 15 s**, `snackbarDuration` 3 s, `zoneEvalMinActivity` 0.05, `repeatSuggestionAfter` 30 s.
  - Construye por mano: `ExcessWrist (1.5/5)`, `LowForearm (1.5/5)`, `ExcessHand (2/5)` (trigger/cooldown en segundos) con criticidades del `HandProfile`.
  - `Evaluate`: respeta warmup + cooldown global (congela engines en cooldown), evalúa cada mano con `GetWindowedSnapshot()` + `GetWindowedActivityRatio()` → `MetricsProcessor.Normalize` + `GetDeviation` → `engine.Evaluate`. Score = `severity × (0.25 + 0.75 × activity)`. La mano más activa gana.
  - Baja actividad se evalúa contra la mano líder (global, `HandType.NONE`). Si actividad < 0.05 resetea el engine (la señal es ruido).
  - Anti-spam: tope 3, cooldown global, deduplicación `mensaje|mano` con rearme 30 s, cooldown interno por regla. Salida: `Debug.Log[SuggestionSystem]` + `SnackbarManager.Show(WARNING)`.
- `SuggestionEngine.cs`: lista de `TimedRule`, devuelve la mayor severidad. `TimedRule.cs`: exige condición sostenida `triggerTime` y aplica cooldown interno.
- Reglas (`rules/`): `ExcessHandRule` / `ExcessWristRule` (`deviation > 1`), `LowForearmRule` (`deviation < -1`), `LowActivityRule` (`activity < threshold`). Severidad `clamp01(|dev| − 1) × criticality`; criticidades por defecto mano 1.0 > muñeca 0.8 > antebrazo 0.6.
- `ExerciseProfile.cs` (`ScriptableObject`): `leftHand/rightHand: HandProfile{ZoneTarget hand/wrist/forearm, criticidades, minActivity}`. `editorValue` 0–100 se normaliza en `OnValidate`; tolerancias con piso anti-división por cero.
- `MetricsProcessor.cs` (estático): `Normalize` (reparto zona/suma; si total ≤ 0.01, ceros) y `GetDeviation = (norm − target)/max(tolerance, 0.0001)`.
- `Suggestion.cs`: `message`, `severity`, `type{Zone, LowActivity}`. DTOs: `AnalysisContext`, `Deviation`, `NormalizedMetrics` (`HandAnalysisContext` es legado).

Sugerencia final baseline: `GeneralSuggestionBuilder` (clase anidada en `Assets/Scripts/Gameplay/SummaryData/SessionRecorder.cs`):

- Insert: `completionTime` (60 s / 120 s). OSU: `TotalInteractionTime` (30 s / 60 s). DuckHunter: `missedRatio` (0.2 / 0.5).
- Se usa como **respaldo** cuando no hay perfil híbrido válido o la calibración no coincide. Ver `docs/scores-persistence.md`.

## Híbrido (`Assets/Scripts/TrackingPipeline/ErgonomicDetection/Suggestions/`)

Event-driven en runtime + batch al final. No modifica agregadores, detectores, métricas ni reglas baseline.

- `HybridSuggestionTrackingSystem.cs` (`sealed`, en `GameManager` de Insert/OSU/DuckHunter): `exerciseProfile` (gobierna `suggestionProfile/calibrationProfile/leftGoal/rightGoal` en `BeginExercise`), `suggestionProfile`, `calibrationProfile`, `leftGoal/rightGoal` (`ObserveOnly` por defecto), `output` (`LogOnly` por defecto). Suscrito a `MotionEventBus.OnFrame`, `ErgonomicExposureEventBus.OnFrame`, `GameManager.OnExcerciseStart/OnExerciseEnd`.
  - Por mano: `UsageInterpreter → HybridFramePairer → SuggestionDecisionOrchestrator`. `LateUpdate` elige entre candidatos L/R vía `HybridSuggestionGate` y publica `HybridSuggestionEventBus` + log detallado (+ snackbar si `LogAndSnackbar`).
  - `OnExcerciseStart` reinicia intérpretes, emparejamiento, reglas y anti-spam. `OnExerciseEnd` deja de procesar y registra emisiones/descartes.
- `UsageInterpreter.cs`: ventana 2048 intervalos, 5 s; exige 3 motions Hand/Wrist/Forearm válidos; integra `value × dt`; participación muñeca = integral muñeca / (muñeca + antebrazo); exige 2 s observados y señal media ≥ 0.01; pondera por tiempo e ignora gaps > umbral, NaN y frames no crecientes.
- `HybridFramePairer.cs`: empareja uso + exposición por mano/`frameId`/`timestamp`; un pendiente por fuente; descarta retrocesos y sobreescrituras sin pareja.
- `SuggestionDecisionOrchestrator.cs` (pura): prioridad ordinal 2 sostenida / 1 acumulada / 0 motor (sin sumar magnitudes). Tipos `NeutralWrist/ReduceTwist/RedistributeToForearm/IncludeWrist`. Coordinación exige hold 2 s + warmup 2 s; protección postural no requiere actividad; alerta histórica no re-avisa en neutral; dimensión inválida bloquea recomendar aumento.
- `HybridSuggestionGate.cs`: tope 3, cooldown 8 s (15 s en Default), dedup por mano×tipo; entre manos gana prioridad, luego duración, luego izquierda; protección actual bloquea aumento de la otra mano.
- `HybridSuggestionProfile.cs` / `HybridExerciseProfile.cs` (`ScriptableObject` en `Assets/Resources/ErgonomicProfiles/`): ventana, observación mínima, señal mínima, bandas 0.35/0.65, hold/warmup/cooldown/máximo, objetivos finales (`wristTarget` 0.40, `forearmTarget` 0.20, tol 0.05 en Insert; OSU/DuckHunter con coordinación desactivada), umbrales de desempeño (Insert 60/120 s, OSU 30/60 s, DuckHunter 0.2/0.5).
- `HybridFinalSuggestionBuilder.cs` (estático): prioriza exposición sostenida → acumulada → objetivos finales de uso (`relativeUsage`, denominador mano+muñeca+antebrazo) + segunda línea de desempeño. Requiere arrays completos/únicos/finitos, suma unitaria, actividad y observación suficientes; usa `maximumSustainedExposureSeconds` (no el episodio actual).
- `HybridExerciseResultSynchronizer.cs`: exige `CaptureUsage` + `CaptureExposure` con misma `duration` y manos correctas, una sola vez por fuente; evita resultados incompletos/duplicados.

Sugerencia final híbrida: `SessionRecorder.SaveExerciseSummary` (en `OnExerciseFinalizing`) usa `HybridFinalSuggestionBuilder` si el perfil valida y la calibración coincide (`calibrationProfileId`); si no, respaldo baseline. Guarda en `ExerciseSummary.generalSuggestion` + `SessionRecorder.LastGeneralSuggestion` (leído por `ResultsUI` y `SessionReader`). Ver `docs/scores-persistence.md`.

## Configuración en Inspector

- Baseline: asignar `ExerciseFeedbackSystem.profile` (idealmente uno por ejercicio; hoy las 3 escenas apuntaban al mismo `Insert_ExerciseProfile`) y `trackingSystem`. Requiere `SnackbarUI` activo para ver snackbars.
- Híbrido: cada escena (Insert/OSU/DuckHunter) tiene `HybridSuggestionTrackingSystem` + tracker de exposición en `GameManager`, con su `HybridExerciseProfile`, `HybridRuntimeProfile` y `ErgonomicCalibrationProfile` en `Assets/Resources/ErgonomicProfiles/` (assets `Default` solo referencia). No cambiar configuración durante una captura.
- `suggestionProfile` y `leftGoal/rightGoal` del componente se conservan por compatibilidad; con `exerciseProfile` asignado mandan los valores del perfil.
- Deshabilitar el componente cancela suscripciones y no reanuda a mitad de ejercicio sin un nuevo inicio.
