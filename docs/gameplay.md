# Gameplay: núcleo, ejercicios, interacción, orientación y preparación

## Núcleo

### `GameManager` (`Assets/Scripts/Gameplay/GameManager.cs`)

Máquina `GAMESTATE{IDLE,COUNTDOWN,PLAYING,RESULTS}` que traduce transiciones a eventos. Campos Inspector: `transition`, `trackingSystem`, `countdown`, `left/right: ArmRuntimeUI`.

- `OnEnable/OnDisable`: `DemoManager.OnDemoClosed → StartCountdown`, `CountdownUI.OnCountdownFinished → OnCountdownFinished` (simétrico).
- `Update`: si `trackingSystem.isTracking`, pinta `left/right.SetData(GetRuntimeSnapshot())`.
- `SetState`: `COUNTDOWN → OnCountdownStart`, `PLAYING → OnExcerciseStart`, `RESULTS → OnShowResults`.
- `EndExercise(duration)`: `OnExerciseEnd → OnExerciseFinalizing → SetState(RESULTS)`. El orden es crítico para métricas y persistencia (ver `docs/architecture.md`).

### `ExerciseController` (`Assets/Scripts/Gameplay/ExerciseController.cs`, abstracto)

Orquestador base de los 3 ejercicios. Requiere `ParticleEffectPlayer`; expone `gameManager`, `progressManager`, `feedbackSystem`, `sessionRecorder`, `exerciseEndDelay` 3 s.

- `OnEnable/OnDisable`: `GameManager.OnExcerciseStart → HandleStartExercise`, `ExerciseProgressManager.OnPhaseCompleted → HandlePhaseCompleted` (+ `ClearEffects`). Los herederos deben llamar a `base`.
- `ExerciseRoutine`: `SessionManager.BeginSession` si no hay sesión → `OnExerciseStart()` → `feedbackSystem.BeginExercise()` → bucle hasta `IsExerciseCompleted` con `feedbackSystem.Evaluate` → espera `exerciseEndDelay` → `OnExerciseEnd(duration)`.
- `OnExerciseEnd`: `SetSpecificData()` (cada ejercicio puntúa y guarda desempeño) → `sessionRecorder.SetPendingScore` → `gameManager.EndExercise`.
- `HandlePhaseCompleted`: efecto de partículas una sola vez por fase (con `HashSet` + validación contra `progressManager`).

### `ExerciseProgressManager` (`Assets/Scripts/Gameplay/ExerciseProgressManager.cs`)

Contador puro sin `Update`. `Initialize(targets)`, `BeginPhase/BeginNextPhase`, `AddCompletedStep(descriptor?)`, `AddMissedStep`, `IsPhase/ExerciseCompleted`. Publica `OnProgressChanged`, `OnManagerInitialized`, `OnPhaseChanged`, `OnPhaseCompleted`, `OnExerciseInitialized/ProgressChanged/ProcessedChanged`, `OnStepCompleted`. Lo consumen runners y UI (`ProgressionUI`, `*PhaseProgressUI`, `VignetteFeedback`, `GameAudioFeedback`).

## Interacción (`Assets/Scripts/Interaction/` + `Assets/Scripts/Gameplay/Interactable.cs`)

Solo Insert y fase 3 usan este camino; OSU/Hunter usan lectura raw + física (ver abajo).

- `InteractionTracker.cs` (`IDisposable`): se suscribe a `MotionEventBus.OnFrame` en el constructor. Filtra por `handType`; traduce a `InteractionEvent{type, phase START/UPDATE/END, handType, strength, frameId, palm pos/rot/vel}`: GRAB por `isActive(GRAB)`, PINCH suprimido si hay grab, ROTATE por `isActive(Wrist)` + valor. Evento instancia `OnInteraction`.
- `InteractionManager.cs` (por mano): crea `InteractionTracker` + `InteractionResolver` en `Awake`; en `OnEnable/OnDisable` suscribe `OnInteraction`. `FindTarget` por `OverlapSphere(palma + rotación×offset, grabRadius, interactableLayer)`; `Resolve` → `Grab (START toma si CanInteract + OnGrabStart, END libera)`, `Rotate (UPDATE → OnRotate)`, `Select (START → OnSelect)`. Contador de fallos Q5: `START` sin target o `!CanInteract` + pieza cercana + debounce → `FailedTakes` (leído por Insert para `left/rightGrabMisses`). `Dispose` en `OnDestroy`.
- `Interactable.cs` (abstracto): base de `PieceBehaviour` y `OrientationPieceBehaviour`. `CanInteract`, `OnGrabStart` (ajuste suave `GrabAnchorSettle`), `OnGrabEnd`, `OnRotate/OnSelect/ForceRelease`, `ClampPosition` por bounding box. Evento `OnForcedRelease`.
- `InteractionResolver.cs`, `InteractionHandData.cs`, `InteractionEvent.cs`, `ResolvedInteraction.cs`: POCOs sin Unity.

## Insert (`Assets/Scripts/Gameplay/Insert/`)

Escena `Assets/Scenes/Insert.unity`. Tomar piezas y encajarlas en slots de pared.

- `WallInsertExercise.cs : ExerciseController`: fases por `InsertPhaseDefinition{prefab, expectedPieces}`. `Start → progressManager.Initialize`; `OnExerciseStart → AdvanceToNextPhase` (`Instantiate`, `ApplyChirality`, `BeginPhase`, `OnPhaseCompositionChanged`, fade). `OnPieceSnapped` valida fase + `AddCompletedStep(PieceStepDescriptor)` + transición con delay + fade. Evento propio `OnPhaseCompositionChanged`. `OnEnable`: base + `PieceBehaviour.OnPieceSnapped`; `OnDisable`: `StopAllCoroutines` + `DeactivateCurrentPhase`.
- `PieceBehaviour.cs : Interactable`: `requiredHand`, `pieceType: SlotType`, estados `Idle/Grabbed/Snapped`. `CanInteract` filtra mano; `OnGrabStart` escala+glow, `OnGrabEnd` restaura; `Snap` bloquea física y emite `OnPieceSnapped`/`OnPieceGrabbed`. Quiralidad en `CountdownUI.OnCountdownFinished`; `Update: ClampPosition`.
- `SlotBehaviour.cs`: `OnTriggerStay` valida tipo/distancia, corutina `AlignAndSnap` interpola a `snapPoint` (+ rotación si `requireRotation`), luego `piece.Snap()` + flash. Highlight estático de slot/pieza.
- Desempeño: `SetSpecificData → scoreAdapter.CompleteExercise(CompletionTime)` + `sessionRecorder.SetInsertPiecesData(CompletionTime, leftMisses, rightMisses)` (misses de `InteractionManager.FailedTakes`).

## OSU (`Assets/Scripts/Gameplay/OSU/`)

Escena `Assets/Scenes/OSU.unity`. Alcanzar objetivos con la mano correcta. **No usa `InteractionManager`**: lee raw vía `GameplayHandInput`.

- `OSUBasedExercise.cs : ExerciseController`: delega todo al runner. `Start → Initialize(sequence.GetPhaseTargets())`; `OnExerciseStart → scoreManager.BeginExercise(OSU) + scoreAdapter.BeginExercise + sequenceRunner.StartSequence(sequence, this)`. `SetSpecificData → SetOsuData(TotalInteractionTime, InteractionCount)`.
- `OSUSequenceRunner.cs`: dueño secuencial, única entrada `StartSequence`. Por fase/paso: `Instantiate(step.prefab)`, `SetColor`, `SetPath` si `TrackingDot`, asigna `detector.target` y suscribe `OnCompleted/Missed/Touched/Failed`. Traduce a `AddCompleted/MissedStep` + `Destroy` + avance con delays. Eventos instancia `OnSequenceStarted`, `OnPhaseCompositionChanged`, `OnTargetProcessed/Spawned/Touched/Completed/Missed/Failed/TrackingStateChanged`.
- `TargetDetector.cs`: polling por frame; si no hit, `TryHit` por `hitRadius`; si hit y es `TrackingDot`, `Track(hand)`. Campos `input: GameplayHandInput`, `target: DotBehaviour`.
- `DotBehaviour.cs` (abstracto) + `HitDotBehaviour` + `TrackingDotBehaviour`: `Hit → OnTouched`, `Complete → OnCompleted`, `Fail → OnFailed`, timeout `timeToInteract → OnMissed`. `HitDot` completa y se destruye; `TrackingDot` sigue curva Bézier (`SetPath`) y falla si la mano sale del radio (`maxContinuous/TotalTimeOutside`).
- Datos: `OSUSequence{pointsDepth, phases[phaseName, steps[stepId, requiredHand, prefab, spawnPosition, path], transitionDelay]}` en `Assets/Resources/OSUSequences/` (`TEST/DEMO/DEFINITIVE`; DEFINITIVE 4 fases 3+6+6+8 con dots móviles) + curvas en `Assets/Resources/OSUPaths/` (10 `PathData`).

## DuckHunter (`Assets/Scripts/Gameplay/Hunter/`)

Escena `Assets/Scenes/DuckHunter.unity`. Apuntar con láser y disparar. Tampoco usa `InteractionManager`.

- `HunterExercise.cs : ExerciseController`: `Start → Initialize(sequence.GetPhaseTargets())`; `OnExerciseStart → scoreManager.BeginExercise(DuckHunter) + sequenceRunner.StartSequence`. Suscribe `OnDuckHit/Missed/SequenceCompleted`. `Hit → AddCompletedStep`, `Missed → AddMissedStep`. `SetSpecificData → SetDuckHunterData(DucksHit, DucksMissed)`.
- `DuckSequenceRunner.cs`: corutina `SequenceRoutine`: por fase `BeginPhase`, por paso `WaitForSeconds(delayBeforeSpawn) → PlaySpawnCue → SpawnDuck(step) → WaitUntil(activeDuck == null)`. `SpawnDuck` instancia, suscribe `OnHit/OnReachedDestination`, `Initialize(side, requiredHand, duration, bounds)`. Cuenta `DucksHit/Missed` y limpia con `BeginDespawn`. Eventos `OnSequenceCompleted/Started`, `OnPhaseCompositionChanged`, `OnDuckProcessed/Spawned/Hit/Missed`.
- `DuckBehaviour.cs`: vuelo `Lerp(start, end, t)` + bob/bank; `t ≥ 1 → OnReachedDestination` (fallo); `Hit(hand)` valida mano, calcula `reactionTime`, emite `OnHit`; `BeginDespawn` con DOTween.
- `HandLaserPointer.cs` (+ `HandPoseListener`, `HandLaserReticle`): `LineRenderer` + `Raycast` contra `Duck+HunterWall`; `poseListener.Aim/Shoot Started/Ended`; `Update` proyecta rayo desde `GameplayHandInput.GetHand(handType)` y al disparar `currentTarget.Hit(handType)`. `HandPoseListener` es emisor puro invocado externamente; `HandLaserReticle` es visual.
- Datos: `DuckSequence{phases[steps[spawnSide, requiredHand, movementDuration, delayBeforeSpawn], transitionDelay]}` en `Assets/Resources/DuckSequences/` (DEFINITIVE 4 fases 2+3+3+8; centro 20 s, laterales 7 s).

### Soporte común gameplay

- `GameplayHandInput.cs`: `dataProvider: LeapDataProvider`; por frame construye `GameplayHandData{position, rotation, velocity}` L/R; `GetHand(type)` con fallback a la más cercana si `NONE`.
- `GameplaySceneLoader.cs : SceneLoader`: `NextExercise → LoadNextScene`.

## Orientación (`Assets/Scripts/Gameplay/UserOrientation/`)

Tres escenas previas; avanzan con `OrientationSceneLoader : SceneLoader` (`phase.OnPhaseCompleted → LoadNextScene`).

- Base `OrientationManager.cs` (abstracto): `OnPhaseCompleted`, `CompletePhase()`.
- Fase 1 (`Phase1/`): exploración + hold con ambas manos en grip + secuencia de 3 volúmenes. `OrientationPhase1Manager` orquesta `HandTracker` (lee `LeapServiceProvider.CurrentFrame`; actividad por deltas, grip con histéresis) + `Exploration` (tiempo activo hasta `requiredActiveTime`) + `Volume` (palma dentro del box tras delay). Eventos `OnProgress/Activity/Grip/Presence/ExplorationCompleted`.
- Fase 2 (`Phase2/`): `OrientationPhase2Manager` instancia `OrientationTarget` y suscribe `OnTouchDetected/OnTouchFeedbackCompleted`; colisión física (sin Leap) con secuencia DOTween; `OnProgressChanged`; corutinas canceladas en `OnDisable`.
- Fase 3 (`Phase3/`): mini-Insert con `OrientationPhase3Manager` (spawn aleatorio, estados `ReadyToGrab/Moving/ReadyToRelease/Completed`), `OrientationPieceBehaviour : Interactable` (agarre vía `InteractionTracker`), `OrientationSlotBehaviour` (solo encaja si suelto + en reposo), `OrientationPhase3FeedbackController` (solo efectos), `OrientationModal` (Continue/Retry).
- UI: `OrientationProgressUI` (iconos fase 2), `OrientationPhase3ProgressUI` (Tomar/Mover/Soltar), `OrientationModal`.

## Preparación (`Assets/Scripts/Gameplay/UserPreparation/`)

Escena `Assets/Scenes/UserPreparation.unity`. Puerta de entrada antes de orientar.

- `HandsDetection.cs`: polling `LeapServiceProvider.CurrentFrame`; `OnLeft/RightHandDetectionChanged`, props `IsLeft/RightDetected`.
- `HandsPreparationFlow.cs`: espera ambas manos → countdown (`countdownSeconds`) con `OnCountdownStep` → `OnPreparationReady/Completed` (revalida presencia). Eventos `OnWaitingForHands`, `OnCountdownStep`, `OnPreparationReady/Completed`.
- `HandsPreparationUI.cs` / `HandsPreparationVisuals.cs`: proyección a canvas + pulso DOTween.
- `PreparationSceneLoader.cs : SceneLoader`: `OnPreparationCompleted → LoadNextScene`.
