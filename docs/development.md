# Desarrollo: entorno, ejecución y depuración

Guía operativa verificada contra `Packages/manifest.json`, `ProjectSettings/`, `Assets/Scenes/` y scripts de arranque.

## Requisitos y primera apertura

- Usar **Unity 2023.1.22f1** (coincide con `ProjectSettings/ProjectVersion.txt`). Otras versiones pueden romper URP 15.0.7 o Ultraleap 7.3.0.
- Windows Standalone, backend Mono, `.NET Standard 2.1` (`ProjectSettings/ProjectSettings.asset`).
- Paquetes clave (`Packages/manifest.json`): Ultraleap 7.3.0 vía OpenUPM, URP 15.0.7, Newtonsoft JSON 3.2.2, TextMeshPro 3.0.9, feature VR, Cinemachine 2.9.7. DOTween se activa por defines y `Assets/Resources/DOTweenSettings.asset`.
- Al abrir, Unity regenera `Library/`, `Temp/` y `*.csproj`. Están ignorados en `.gitignore` junto a `Obj/`, `Builds/`, `Logs/`, `UserSettings/`, `*.sln`, `*.user`, `*.pdb`. No comprometerlos. Los cambios preexistentes en `Library/` o `Temp/` no deben versionarse.
- Cada script/asset nuevo bajo `Assets/` exige su `.meta` (ver `AGENTS.md`). No crear `.cs` sueltos esperando que compilen sin reimportar: Unity debe regenerar el `.csproj`.

## Escenas y orden

Orden real en `ProjectSettings/EditorBuildSettings.asset` (índice 0 deshabilitado):

1. `SampleScene.unity` — `enabled: 0` y **fichero inexistente** (huérfana; ignorar).
2. `Assets/Scenes/Initial.unity` — entrada. `InitialScreenController` + `RegisterForm` + `Transition`.
3. `Assets/Scenes/UserPreparation.unity` — `HandsPreparationFlow` exige ambas manos antes de continuar.
4. `Assets/Scenes/UserOrientationPhase1/2/3.unity` — orientación progresiva.
5. `Assets/Scenes/Insert.unity` — ejercicio manipulativo (nombre real; no `Insert_Config1.unity`).
6. `Assets/Scenes/OSU.unity` — objetivos estáticos y móviles.
7. `Assets/Scenes/DuckHunter.unity` — láser + patos.
8. `Assets/Scenes/SessionSummary.unity` — métricas articulares + exposición.
9. `Assets/Scenes/ScoresSummary.unity` — puntuaciones + leaderboard.

Fuera de build: `Assets/Scenes/Cuaterniones prueba.unity` (pruebas de rotación, no flujo paciente).

Para probar un ejercicio aislado, abrir su escena y pulsar Play, pero sin pasar por `Initial` no habrá usuario/sesión y `SessionManager` intentará crearla al arrancar el ejercicio (ver `Assets/Scripts/Gameplay/ExerciseController.cs`). El flujo soportado es empezar en `Initial`.

## Ejecutar y probar

1. Conectar el Leap Motion. `LeapControllerInitialization` (`Assets/Scripts/Connection/LeapControllerInitialization.cs`) fija `targetFrameRate 60`, muestra snackbar `Esperando Leap...` (1000 s) y al conectar emite `OnLeapConnected` + `ChangeTrackingMode(Desktop)`.
2. Pulsar Play en `Initial.unity`, registrar/seleccionar usuario y avanzar a `UserPreparation`.
3. En cada ejercicio, `CountdownUI` (3 s + `¡VAMOS!`) da tiempo a colocar manos y fija quiralidad (`PieceBehaviour` la configura en `OnCountdownFinished`).
4. Durante `PLAYING`, `GameManager.Update` pinta `ArmRuntimeUI` si `trackingSystem.isTracking`.
5. Al terminar, `RESULTS` muestra `ScoresUI` + `TrophyView` + `ResultsUI` (botón Métricas alterna).

Atajos y debug verificados:

- `R`: recarga la escena (`Assets/Scripts/Utils/SceneLoader.cs`).
- `1/2/3`: saltan a Insert/OSU/Hunter si `Assets/ExcerciseNavigator.cs` está en escena (solo debug).
- `0`: reinicia pieza (`PieceBehaviour`); `A/S/D` en Editor prueban snackbars (`ConnectionManager`).
- `MotionDebugLogger` (opcional en escena) loguea cada `FrameMotionData` con motions activos y gestos.

## Compilar por CLI

```powershell
dotnet build "Hands.sln" --no-restore
```

Casos:

- `NETSDK1004` tras limpiar `Temp/`: faltan `project.assets.json`; repetir sin `--no-restore`.
- `MSB4166` (nodo MSBuild cerrado): repetir con `-m:1`:

```powershell
dotnet build "Hands.sln" -m:1
```

`Assembly-CSharp.csproj` es el proyecto del juego. Si se añade un `.cs` nuevo y no aparece, reabrir Unity para que regenere proyectos antes de compilar.

No hay tests propios que ejecutar: `com.unity.test-framework` está instalado pero los únicos `*Test*.cs` son del plugin `Assets/UIExtensions`. La verificación es compilar + escena de ejercicio con Leap.

## Depuración por subsistema

- **Sin frames**: comprobar `LeapServiceProvider` conectado, `LeapDataProvider` en escena y suscripción de `MotionPipelineRunner` a `OnFrameReady`. `MotionPipelineRunner` descarta si movimiento y ergonomía no están sincronizados (`frameId`/`handType`/`timestamp`) con `LogError`.
- **Métricas a cero**: `MetricsTrackingSystem.isTracking` solo es `true` entre `OnExcerciseStart` y `OnExerciseEnd`. Fuera de `PLAYING`, `OnFrameReceived` retorna. Revisar `GetRuntimeSnapshot` (acumulado) vs `GetWindowedSnapshot` (ventana 5 s) según el consumidor.
- **Sin sugerencias en vivo**: baseline requiere `ExerciseProfile` asignado + `warmupTime` superado + actividad mínima (`zoneEvalMinActivity` 0.05) + `SnackbarUI` activo (si no, solo `Debug.Log`). Híbrido requiere `HybridExerciseProfile` + calibración coincidente; por defecto `output = LogOnly` (no toca la pantalla del baseline).
- **Sin sugerencia final**: `SessionRecorder.SaveExerciseSummary` se suscribe a `OnExerciseFinalizing`. Si falta uso (`TryFinalize` falso) o la calibración no coincide, guarda respaldo de desempeño o `No hay datos suficientes...`. Leer `LastGeneralSuggestion` en `ResultsUI`.
- **Sin persistencia**: `SessionManager`/`ExerciseResultPersistenceService` exigen usuario seleccionado y 3 ejercicios con mismo `sessionGuid`. Revisar `exercise-commit.json` (journal) en `persistentDataPath/app/profiles/{userId}/`.
- **Sin sonido**: `AudioManager` es singleton `DontDestroyOnLoad` con `AudioLibrary.asset`; cada feedback hace `Play` nulo-seguro. Comprobar `AudioType` mapeado en el asset.
- **Escena que no avanza**: cada loader deriva de `SceneLoader` y espera su evento (`OnPhaseCompleted`, `OnPreparationCompleted`, `OnDemoClosed`, etc.). Revisar `-=`/`+=` simétricos y `nextSceneName` en Inspector.

Logs con prefijo (`[SuggestionSystem]`, `[HybridSuggestions]`, `[ScoreSystem]`, `Insert:/OSU:/Hunter:`) y `Debug.LogError` ante configuración faltante (prefab/asset nulo) ayudan a localizar el subsistema.

## Configuración que no auto-cablea

Asignar en Inspector (ver `AGENTS.md` y `docs/ui-systems.md`):

- `ResultsUI.generalSuggestionText` en las 3 escenas de ejercicio.
- `SessionReader.generalSuggestionText` en `SessionSummary.unity`.
- `ExerciseFeedbackSystem.profile` + `trackingSystem`; `MetricsTrackingSystem` en `GameManager.trackingSystem`.
- `HybridSuggestionTrackingSystem.exerciseProfile`; `ErgonomicExposureTrackingSystem.calibrationProfile` (debe ser el mismo asset que referencia el perfil del ejercicio); `SessionRecorder.exerciseType + hybridProfile`.
- `ScoreManager` configs + `classificationCatalog`; adapters suscritos a sus runners.

## Problemas conocidos

- Referencia huérfana a `SampleScene.unity` en build settings (no existe; dejar `enabled: 0`).
- Tres escenas comparten el mismo `Insert_ExerciseProfile`; crear un perfil por ejercicio si se ajustan targets.
- `suggestionCooldown` real por defecto 15 s (código), aunque guías antiguas citen 8 s.
- `isActive` en `HandProfile` es informativo; ambas manos se evalúan siempre.
- `ScorePersistenceListener` está obsoleto (ver atributo en `Assets/Scripts/Persistence/ScorePersistenceListener.cs`); la persistencia real es `ExerciseResultPersistenceService`.
- `RegistrationManager.cs` (raíz de `Scripts/`) es legado; el flujo real usa `InitialScreenController` + `RegisterForm` + `UserService`.
