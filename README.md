# Hands

Prototipo Unity de prevención motora del miembro superior con **seguimiento de manos sin contacto** (Ultraleap Leap Motion).
El paciente realiza ejercicios gamificados mientras el sistema **cuantifica el uso articular** (mano, muñeca, antebrazo) y **guía** con sugerencias en vivo y finales.

Tres metas: **ejercitar** (Insert, OSU, DuckHunter), **medir** (tiempo activo, zonas usadas, intensidad por mano) y **guiar** (feedback inmediato + sugerencia final por ejercicio y sesión).

## Requisitos

Obligatorio:

- **Unity Editor 2023.1.22f1** (ver `ProjectSettings/ProjectVersion.txt`). URP 15.0.7.
- **Windows Standalone** (backend Mono, `.NET Standard 2.1`; ver `ProjectSettings/ProjectSettings.asset`).
- **Dispositivo Leap Motion + servicio Ultraleap** conectado. Sin él, `LeapControllerInitialization` muestra `Esperando Leap...` y no hay frames.
- **Git** con el repo clonado. No se requieren variables de entorno ni secretos: no hay `GetEnvironmentVariable` en `Assets/Scripts` y la persistencia usa `Application.persistentDataPath` (ver `Assets/Scripts/Persistence/SaveSystem.cs`).

Dependencias Unity (ver `Packages/manifest.json`):

- `com.ultraleap.tracking` 7.3.0 (registro OpenUPM), `com.unity.render-pipelines.universal` 15.0.7, `com.unity.nuget.newtonsoft-json` 3.2.2 (persistencia JSON), TextMeshPro, UGUI, feature VR / XR, Cinemachine, Test Framework.
- DOTween por `scriptingDefineSymbols: DOTWEEN` y `Assets/Resources/DOTweenSettings.asset` (no es paquete UPM).

## Instalación

1. Clonar el repositorio y abrir la carpeta raíz en **Unity Hub** con 2023.1.22f1.
2. Dejar que Unity resuelva paquetes y regenere `Temp/`, `Library/` y `*.csproj`. Esos generados están ignorados (`Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.sln` en `.gitignore`); no comprometerlos.
3. Conectar el Leap Motion antes de pulsar Play.
4. Abrir la escena `Assets/Scenes/Initial.unity` (punto de entrada real según `ProjectSettings/EditorBuildSettings.asset`).

No hay instaladores, migraciones ni `.env`. Los datos de juego se crean en runtime bajo `{persistentDataPath}/app/` (ver `docs/scores-persistence.md`).

## Ejecución

Orden de escenas en build (`ProjectSettings/EditorBuildSettings.asset`):

1. `Initial.unity` — registro/selección de usuario (`InitialScreenController`, `RegisterForm`). Botón inicio → `UserPreparation`.
2. `UserPreparation.unity` — detección de ambas manos (`HandsPreparationFlow`).
3. `UserOrientationPhase1/2/3.unity` — familiarización (exploración, tocar objetivos, tomar-mover-soltar).
4. `Insert.unity`, `OSU.unity`, `DuckHunter.unity` — los tres ejercicios (orden de sesión habitual Insert → OSU → DuckHunter).
5. `ScoresSummary.unity` + `SessionSummary.unity` — puntuaciones y métricas articulares de la sesión.

Flujo en cada ejercicio (`Assets/Scripts/Gameplay/GameManager.cs`, `Assets/Scripts/Gameplay/ExerciseController.cs`):

`Demo cerrada → CountdownUI (3 s + ¡VAMOS!) → GAMESTATE.PLAYING → ExerciseRoutine (métricas + feedback en vivo) → EndExercise(duration): OnExerciseEnd → OnExerciseFinalizing → RESULTS (OnShowResults)`.

Atajos verificados en código:

- `R` recarga la escena activa (`Assets/Scripts/Utils/SceneLoader.cs`).
- `Alpha1/2/3` saltan a Insert/OSU/Hunter solo si `Assets/ExcerciseNavigator.cs` está en escena (debug, fuera del flujo de paciente).
- `0` reinicia la pieza en `PieceBehaviour` (debug); `A/S/D` prueban snackbars en Editor (`ConnectionManager`).

Cada ejercicio tiene su variable de desempeño (ver `docs/gameplay.md`): Insert `CompletionTime`, OSU `TotalInteractionTime + InteractionCount`, DuckHunter `DucksHit/DucksMissed`.

## Compilación y verificación

Desde la raíz del proyecto:

```powershell
dotnet build "Hands.sln" --no-restore
```

Si falla con `NETSDK1004` (falta `Temp/obj/**/project.assets.json`, p. ej. tras limpiar `Temp/`) o `MSB4166` (nodo MSBuild cerrado), usar sin `--no-restore` y con un solo nodo:

```powershell
dotnet build "Hands.sln" -m:1
```

Notas (ver `AGENTS.md`):

- `Assembly-CSharp.csproj` contiene los scripts del juego; los `.csproj` se regeneran en `Temp/`.
- Al añadir un `.cs` nuevo, Unity debe reimportar para incluirlo; por eso varias clases auxiliares viven anidadas en archivos ya incluidos (p. ej. `GeneralSuggestionBuilder` en `SessionRecorder.cs`).
- Todo script/asset nuevo bajo `Assets/` necesita su `.meta`.

No hay suite de tests propia: los únicos `*Test*.cs` pertenecen a `Assets/UIExtensions` (plugin de terceros). La verificación es compilar + probar en Editor con Leap conectado.

## Documentación

- [docs/architecture.md](docs/architecture.md) — arquitectura, flujos y eventos entre subsistemas.
- [docs/development.md](docs/development.md) — entorno, escenas, depuración y resolución de problemas.
- [docs/tracking-metrics.md](docs/tracking-metrics.md) — captura Leap, pipeline de movimiento, métricas y exposición ergonómica.
- [docs/suggestions.md](docs/suggestions.md) — sistema base de sugerencias e híbrido (resumen; detalle híbrido en `Assets/Scripts/TrackingPipeline/ErgonomicDetection/Suggestions/README.md`).
- [docs/gameplay.md](docs/gameplay.md) — `GameManager`, ejercicios, interacción, orientación y preparación.
- [docs/scores-persistence.md](docs/scores-persistence.md) — puntuaciones, persistencia JSON y resumen de sesión.
- [docs/ui-systems.md](docs/ui-systems.md) — UI, audio, conexión, utilidades y configuración (`Resources`, `ProjectSettings`, `Packages`).
- `AGENTS.md` — contexto del proyecto y convenciones obligatorias para agentes y contribuidores (event-driven, `[SerializeField] private`, `-=` garantizado, etc.).
- `Presentacion_MotiVa_Pruebas_Usuario.pptx` — presentación de pruebas de usuario (no normativa técnica).

## Limitaciones conocidas

- `EditorBuildSettings.asset` referencia `Assets/Scenes/SampleScene.unity` deshabilitada cuyo fichero **no existe** en disco (referencia huérfana; no afecta al build).
- La escena de Insert se llama `Assets/Scenes/Insert.unity` (no `Insert_Config1.unity`).
- Las tres escenas de ejercicio apuntaban al mismo `Insert_ExerciseProfile` (guid `985222d45ab32e24c9709c8c8ac63a27`); se recomienda un perfil por ejercicio.
- `ExerciseFeedbackSystem.suggestionCooldown` por defecto es **15 s** en código (`Assets/Scripts/SuggestionSystem/ExerciseFeedbackSystem.cs`), no 8 s.
- `SnackbarManager.Show` es no-op si no hay un `SnackbarUI` activo en la escena.
- Coexisten dos caminos de tracking: pipeline desacoplado (`MotionEventBus`, usado por Insert/fase 3) y lectura raw directa del Leap (`GameplayHandInput` en OSU/Hunter, `LeapServiceProvider` en orientación/preparación). Ver `docs/architecture.md`.
- El giro palma–antebrazo del SDK no está validado como pronación/supinación anatómica; los umbrales ergonómicos son provisionales del prototipo, no RULA clínico.
