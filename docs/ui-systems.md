# UI, audio, conexión, utilidades y configuración

## UI (`Assets/Scripts/UI/` + pantallas)

- **Snackbar** (`UI/Snackbar/`): `SnackbarManager` (estático; `Show(type, msg, time = 2 s)`, `OnShow`; no-op sin suscriptor) + `SnackbarUI` (`bg`, `message`, `CanvasGroup`; colores error/warning/success; `DOFade` + auto-hide). Requiere instancia activa para ver sugerencias y avisos Leap. Lo usan sugerencias, `ConnectionManager` y `LeapControllerInitialization`.
- **Countdown** (`UI/CountdownUI.cs`): escucha `OnCountdownStart`; cuenta `countdownTime` 3 + `¡VAMOS!`, `AudioManager.Play(CountdownTick)`, fade y `OnCountdownFinished` (arranca `PLAYING` y fija quiralidad en Insert).
- **Tabs** (`UI/Tabs/`): `TabGroup(tabs, contents)` valida conteos, selecciona la primera en `Start` y re-anima `ArmResultUI` al cambiar; `CustomTabButton` (`IPointerClickHandler`, `OnTabClicked/Selected`; `SetSelected` sin cambio visual implementado).
- **Brazos**: `UI/Arm/ArmUI.cs` (abstracto; `hand`, gradiente, `ApplyColor`), `ArmRuntimeUI.SetData(RuntimeMetrics)` (filtra por mano), `ArmResultUI.Paint(summary)` + `PlayAnimation` (0.5 s, `Clamp01 + Pow(1.3)`).
- **Progresión**: `UI/ProgressionUI.cs` (4 fases genérico; `OnPhaseChanged/Completed`) + `UI/Progression/InsertPhaseProgressUI.cs` (tira de piezas fase activa; `OnPhaseCompositionChanged/StepCompleted/PhaseCompleted`; iconos por `PieceIconCatalog(SlotType → sprite)` + `HandsColor`) + equivalentes OSU/Duck.
- **Resultados**: `UI/Results/ResultsUI.cs` + `ResultsManager.cs` (ver `docs/scores-persistence.md`).
- **Scores**: `ScoresUI`, `ScoresSummary*`, `ScoreCardUI`, `LeaderboardView` (ver `docs/scores-persistence.md`).
- **Vignette/feedback**: `UI/Feedback/VignetteFeedbackEffect.cs` (fallo α .22 en `OnTargetMissed/Failed/OnDuckMissed`, éxito α .12 en `OnPhaseCompleted`; fade .06/.4 s) + `ParticleEffectPlayer`, `InsertPieceFeedback`, `InsertPhaseFeedback` (partículas al `Snap`/fase).
- **Orientación**: `UI/UserOrientation/OrientationModal.cs` (`OnPhaseCompleted → Open`; Continue → `sceneName`, Retry → Phase1; `Transition.FadeIn`), `OrientationProgressUI.cs` (iconos fase 2 por filas de 3), `OrientationPhase3ProgressUI.cs` (Tomar/Mover/Soltar con rich-text).
- **Entrada/registro**: `UI/InitialScreenController.cs` (escena `Initial`; `startButton → UserPreparation`, dropdown perfiles, oculta inicio sin registro; `OnCurrentUserChanged`) + `UI/RegistrationForm/RegisterForm.cs` (`nameField`, dropdowns año/mes/día es-ES desde 1900, `UserService.Register`).
- **Resumen**: `SummaryView/SummarySceneLoader.cs` + `SummaryUIActions.cs` (`NewSession/UserPreparation/ExitApp`); `GameplaySceneLoader.NextExercise`; loaders de orientación/preparación por eventos.

## Audio (`Assets/Scripts/Audio/` + `Assets/Resources/AudioLibrary.asset`)

- `AudioManager.cs` (singleton `DontDestroyOnLoad`): `audioLibrary + masterVolume + audioSource`, loops por `AudioType`; fachada estática nulo-segura `Play/PlayLoop/StopLoop/StopAll`; `BuildLookup` desde el asset.
- `AudioLibrary.cs` (`CreateAssetMenu Hands/Audio Library`): `List<AudioDefinition{type, clip, volume}>`; `OnValidate` detecta `None`/duplicados. Asset con 19 entradas mapeadas (tipos 1,2,4,5,9–15,17–24).
- `AudioType.cs`: `None 0, ButtonClick 1, CountdownTick 2, CountdownFinished 3, ExerciseStart 4, ExerciseCompleted 5, Success 6, Warning 7, Error 8, OsuHaloTimer 9, LaserShot 10, PieceSnapped 11, PhaseCompleted 12, ExerciseAmbience 13, MenuTheme 14, DuckHit 15, OsuTargetHit 17, OsuTargetFailed 18, PieceGrabbed 19, DuckEscape 20, TrophyGold/Silver/Bronze 21–23, OrientationTargetPop 24`.
- Puentes event-driven (auto-suscripción, sin Inspector salvo indicado):
  - `GameAudioFeedback`: `OnExcerciseStart → ExerciseStart + loop Ambience`, `OnExerciseEnd → stop + Completed`, `OnPhaseCompleted → PhaseCompleted`, `OnPieceSnapped/Grabbed`.
  - `UIAudioFeedback`: clicks y tabs; opcional loop `MenuTheme`.
  - `OsuAudioFeedback` (requiere `OSUSequenceRunner`): spawn → loop `HaloTimer`; touched/completed/missed/failed → stop + hit/fail.
  - `HunterAudioFeedback` (`DuckSequenceRunner + HandPoseListener[]`): disparo → `LaserShot`; hit → `DuckHit`; missed → `DuckEscape`.
  - `OrientationAudioFeedback`: loop Ambience, fase → `PhaseCompleted`, encaje fase 3 → `PieceSnapped`, progreso fase 2 → `TargetPop`.

## Conexión y errores (`Assets/Scripts/Connection/`, `ErrorHandling/`)

- `LeapControllerInitialization.cs`: `provider + Controller`; `OnLeapConnected/Disconnected`; `Start → InitializeLeap` (snackbar `Esperando Leap...` 1000 s → `WaitUntil Connected` + 0.5 s → `OnLeapConnected` + `ChangeTrackingMode(Desktop)`); `Update` detecta flancos `IsConnected`; `targetFrameRate 60`.
- `ConnectionManager.cs`: `OnLeapConnected → SUCCESS`, `OnLeapDisconnected → ERROR`; en Editor `A/S/D` prueban snackbars.
- `ErrorHandling/ErrorEventArgs.cs`: único POCO `{errormsg}`; no hay bus central: los errores van por `Debug.LogError/Warning` con prefijo + `SnackbarManager.Show(ERROR)`.

## Utilidades (`Assets/Scripts/Utils/` + raíz)

- `SceneLoader.cs` (base): `nextSceneName`, `transition`, `transitionMessage`; `Start → FadeOut`; `Update: R → ReloadScene`; `LoadNextScene` con `FadeIn(callback, 3 s)`.
- `Transition.cs` (requiere `CanvasGroup`): `FadeOut` (α→0), `FadeIn(onComplete, hold 2 s)` con guarda `isTransitioning`, `SetMessage`, evento `OnFadeOutCompleted`.
- `Enums.cs`: `ExerciseType`, `HandType`, `GestureType/Phase`, `InteractionType`, `MotionZone`, `ERRORTYPE`, `TRACKINGSTATE`, `SNACKBARTYPE`, `SlotType{SQUARE,CIRCLE,STAR,DONUT,Line,S,Z,L,Three,U}`, `GAMESTATE`, `SpawnSide`.
- `HandsColor.cs`: `Left` azul, `Right` naranja, `Default` blanco.
- `UsageData.cs`: POCO legado (vigente: `RuntimeMetrics`/`HandUsageSummary`).
- `RegistrationManager.cs` (legado): muestra/oculta panel según `UserService.IsRegistered`; flujo real en `InitialScreenController`.
- `Assets/ExcerciseNavigator.cs` (nótese typo; debug): `Alpha1/2/3 → LoadScene(insert/osu/hunter)`.

## Configuración y recursos

- `Packages/manifest.json`: ver `docs/development.md` (Ultraleap 7.3.0, URP 15.0.7, Newtonsoft 3.2.2, etc.).
- `ProjectSettings/`: `ProjectSettings.asset` (producto `Hands`, compañía `DefaultCompany`, bundle 0.1.0, Linear, Forward, Mono, `Both` input, appId plantilla URP), `GraphicsSettings` (pipeline URP), `QualitySettings` (actual `High Fidelity`), `AudioManager` (stereo, DSP 1024), `TagManager` (tag `Slot_Cube`; layers `GrabbableLayer`, `PhysicalHands`, `DuckLayer`, `TrophyLayer`, `HunterWall`, ...), `TimeManager` (fixed 90 Hz), `DynamicsManager` (gravedad −9.81, `QueriesHitTriggers`), `XRSettings` + `EditorBuildSettings` (`Oculus.Settings`, loader XR, OpenXR), `URPProjectSettings` (stub; detalle en `Assets/Settings/`).
- `Assets/Resources/`: `Ultraleap Settings.asset`, `DOTweenSettings.asset`, `AudioLibrary.asset`, `ScoreClassificationCatalog.asset` + `DefaultScoreClassificationProfile.asset` (bandas 0/60/80/95), `OSUSequences/` (TEST/DEMO/DEFINITIVE), `OSUPaths/` (10 `PathData`), `DuckSequences/` (TEST/DEMO/DEFINITIVE), `ErgonomicProfiles/` (12 assets: 4 calibración, 4 runtime, 3 ejercicio + Default; ver `docs/tracking-metrics.md` y `docs/suggestions.md`).
- Perfiles baseline fuera de `Resources`: `Assets/Scripts/SuggestionSystem/Profiles/Insert/OSU/Hunter_ExerciseProfile.asset` (p. ej. Insert 40/40/20 tol 0.1, criticidad 1/0.8/0.6, `minActivity` 0.4; OSU 10/80/10, `minActivity` 0.2).
- `.gitignore`: ignora `Library/Temp/Obj/Builds/Logs/UserSettings`, `*.csproj/*.sln`, binarios de build y `MemoryCaptures/Recordings`.
