# Puntuación, persistencia y resumen de sesión

## Puntuación (`Assets/Scripts/ScoreSystem/`)

Escala 0–100 por ejercicio + trofeo + desglose. Orquestación `ScoreManager → ScoreCoordinator → calculadora → ScoreEventBus`.

- `ScoreManager.cs` (`MonoBehaviour`): fachada Inspector (`insertConfig`, `osuConfig`, `duckHunterConfig`, `classificationCatalog`). `BeginExercise(type)`, `CompleteInsert/OSU/DuckHunter(data) → LastScore`, `Reset`, `IsSessionActive`.
- `ScoreCoordinator.cs` (POCO): máquina `Begin → Complete → Reset` sobre `IScoreSession`; al completar válido publica `ScoreEventBus`.
- `ScoreSession.cs` / `IScoreSession.cs` / `IScoreCalculator.cs`: sesión activa + interfaz `Calculate(input) → ExerciseScore`.
- `ScoreEventBus.cs`: filtra nulos/inválidos/no-finitos, `ClampScore` 0–100 y publica `OnScoreCompleted`. Lo consume `ScoresUI`.
- `ExerciseScore.cs` (+ `ScoreResultFactory`): `exerciseType`, `totalScore`, `trophyTier`, `classificationProfileVersion`, `breakdown`, `isValid`, `statsData{exerciseDuration, hits, misses}`. `totalScore` es media ponderada del desglose; exige peso positivo y valores finitos.
- `ScoreMath.cs`: `IsFinite`, `ClampScore`, `NormalizeLowerIsBetter(ratio, excellent, maximum)`, media ponderada.
- `ScoreBreakdown.cs`: `struct{metricId, rawValue, metricScore, weight}`.
- `ScoreData.cs`: entradas `InsertScoreData{completionTime, totalPieces, completedPieces, phaseCount, phaseTimes}`, `OSUScoreData{exerciseDuration, totalTargets, completedTargets, missedTargets, totalReactionTime, totalTimeOutsidePath, targets}`, `DuckHunterScoreData{exerciseDuration, totalDucks, ducksHit/Missed, totalReactionTime, ducks}`.
- `ScoreConfigs.cs` (valores verificados): Insert `{excellent 1, maximum 2, base 5, perPiece 5, rotationExtra 0, time .75, completion .25}`; OSU `{reaction 1, tracking .5, missPenalty 1, reaction .4, tracking .3, completion .3}`; DuckHunter `{reaction 1, missPenalty 1, reaction .35, accuracy .65}`.
- Calculadoras: `InsertScoreCalculator` (tiempo esperado = base + piezas×porPieza + fases×extra; si incompleto penaliza tiempo), `OSUScoreCalculator` (reacción + fuera de ruta + penalización por fallo), `DuckHunterScoreCalculator` (reacción + precisión). Retornan inválido ante nulos/negativos/no-finitos/ceros; normalizan conteos que exceden el total.
- Adapters (puente gameplay→score, `MonoBehaviour`):
  - `InsertScoreAdapter`: `BeginExercise(totalPieces, phaseCount)`, `Begin/EndPhase`, `RecordPieceSnapped` filtrado por fase, `CompleteExercise(completionTime)`.
  - `OSUScoreAdapter`: suscrito a `OSUSequenceRunner` (`Spawned/Touched/Completed/Missed/Failed/TrackingStateChanged`); acumula reacción, fuera de ruta y conteos.
  - `DuckHunterScoreAdapter`: suscrito a `DuckSequenceRunner` (`Spawned/Hit/Missed`); `BuildData(duration)` → `CompleteDuckHunter`.
- Clasificación: `ScoreClassificationProfile.cs` (`CreateAssetMenu`; `profileVersion ≥ 1`, bandas `minimumScore → grade/trophy`; exige banda 0 y cortes únicos) + `ScoreClassificationCatalog.cs` (3 perfiles; asset `Assets/Resources/ScoreClassificationCatalog.asset` + `DefaultScoreClassificationProfile.asset`). `TrophyTier.cs`: `Bronze/Silver/Gold/None`.
- `ScoreDisplayFormatter.cs`: `ORO/PLATA/BRONCE/-`, `85/100`, breakdown por líneas. `ScoreSystemLog.cs`: `Console.WriteLine` (no `Debug.Log`).
- UI de score: `ScoresUI.cs` (panel por ejercicio; `OnShowResults → Display`, `OnScoreCompleted` rellena total/desglose/slider/statCards; botón Métricas → `ResultsManager.OpenResults`), `ScoreCardUI.cs` + `BreakdownRowUI.cs` (tarjetas/filas en resumen), `LeaderboardView.cs` + `LeaderboardDataController.cs` (top 5 + fila usuario si rank > 5; `mm:ss` o `---`), `ScoresSummaryController.cs` (pantalla `ScoresSummary`: promedio, panel principal, 3 tarjetas, desglose, tabs por ejercicio → `OnExerciseSelected`), `TrophyView.cs` (3 objetos + cámara/render texture; `Show(tier)` + `OnTrophyLanded` + pulso) + `TrophyAudio.cs` + variante `ScoresSummaryTrophyView.cs`.

`ScoreExerciseType{Insert,OSU,DuckHunter}` es distinto de `ExerciseType` de gameplay; el mapeo vive en `ExerciseResultIdentity`.

## Persistencia (`Assets/Scripts/Persistence/`)

JSON indentado (Newtonsoft) con escritura atómica (`*.GUID.tmp` + `File.Replace/Move` + `Flush(true)`). Sin backend, sin secretos. Ver `SaveSystem.cs`, `SaveFiles.cs`, `PersistenceManager.cs`.

Rutas físicas (`SaveSystem.GetPath`):

- Global: `{Application.persistentDataPath}/app/{fichero}.json` → `users.json`, `active-user.json`, `InsertLeaderboard.json`, `OsuLeaderboard.json`, `HunterLeaderboard.json`.
- Por perfil: `{...}/app/profiles/{userId}/{fichero}.json` → `user.json`, `sessions.json`, `scores.json`, `expositions.json`, `exercise-commit.json` (journal).

Nombres lógicos en `SaveFiles.cs`: `users`, `active-user`, `user`, `settings`, `sessions`, `scores`, `expositions`, `exercise-commit`, `InsertLeaderboard`, `OsuLeaderboard`, `HunterLeaderboard`.

- `PersistenceManager.cs` (singleton `DontDestroyOnLoad`): dueño de `UserService`, `SessionService`, `ScoreService`, `LeaderboardService`, `ExpositionServices`, `ExerciseResultService`. Valida el catálogo en `Awake`; al cambiar de usuario limpia sesión, recontextualiza por `userId` y reintenta `RecoverPendingTransaction()`.
- `SaveSystem.cs` (estático): única I/O (`Save/Load/Exists/Delete` global y por usuario; `Load` retorna `default` si no existe).
- Modelos (`Model/`): `UserData/UserProfile/UsersData/ActiveUserData`; `SessionsData{List<SessionSummary>}` (tipo `SessionSummary` en `Gameplay/SummaryData/SessionSummary.cs`: `SessionId`, `SessionGuid`, `List<ExerciseSummary>`, `date`); `ScoresData{List<ScoreRecord>}` (`ScoreRecord{sessionGuid, sessionIdNumeric, exerciseIndex, exerciseType, recordedAt, totalScore, trophyTier, profileVersion, isValid, statsData, breakdown}`); `LeaderboardData{List<LeaderboardEntry>}` (`UserId/UserName/Score/Duration/RecordedAt/SessionGuid/Trophy`); `ExpositionSummary{sessionGuid/sessionId/exerciseIndex/exerciseType/duration, leftHand/rightHand: HandExpositionSummary}` (3 dimensiones con `validObservation`, `maximumSustained`, `cumulative`, `sustained`, flags); `ExerciseResultTransaction` + `ExerciseResultIdentity` (`RequiredExerciseCount = 3`, clave `sessionGuid|exerciseType`, exige Insert+OSU+DuckHunter distintos, equivalencia por JSON).
- Servicios (`Services/`):
  - `UserService`: registro/selección multi-perfil (`Register` genera Guid; persiste `user/users/active-user`); evento `OnCurrentUserChanged`.
  - `SessionService`: cache por `userId` en `sessions.json`; `UpsertSession` solo acepta sesión **completa** (3/3) e idempotente por `SessionGuid` (exige equivalencia JSON); `GetLastSessions(7)`, `PeekNextSessionId = Max + 1`.
  - `ScoreService`: `scores.json` + migración que rellena trofeo/versión si 0; `UpsertScore` valida finito ≥ 0 y clave `(sessionGuid, exerciseType)` idempotente.
  - `LeaderboardService`: 3 leaderboards **globales** en memoria y JSON; `UpdateHighscore` solo si supera; orden `Score desc, fecha asc, userId`; migra clasificación al cargar.
  - `ExpositionServices`: `expositions.json`; clave `(sessionGuid, exerciseType)`; valida índices, duración finita y manos/dimensiones correctas.
  - `ExerciseResultPersistenceService`: **coordinador transaccional único**. Acumula pendientes por `idempotencyKey`; con < 3 retorna `Pending`; con 3 escribe journal `exercise-commit.json` y aplica `UpsertSession + UpsertScore×3 + UpsertExposition×3 + UpdateHighscore×3`, borra journal y retorna `Committed`. Detecta `Duplicate/Conflict/Rejected/Failed`; `RecoverPendingTransaction` reaplica journal válido del mismo usuario; `DiscardPendingSession`, `SetUserContext`.
- `ScorePersistenceListener.cs`: **obsoleto** (atributo en código); no usar.

## Sesión y resumen (`Assets/Scripts/Gameplay/SummaryData/` + UI)

- `SessionRecorder.cs` (por escena de ejercicio; Inspector `exerciseType + hybridProfile`): en `OnEnable` suscribe `MetricsTrackingSystem.OnTrackingStop → results.CaptureUsage`, `ErgonomicExposureEventBus.OnTrackingStop → results.CaptureExposure`, `OnExcerciseStart → BeginExercise`, `OnExerciseFinalizing → SaveExerciseSummary`.
  - `BeginExercise` limpia buffers (`totalInteractionDelay/interactionCount`, `ducksHit/Missed`, `completionTime`, `grabMisses`, `pendingScore`) y `LastGeneralSuggestion`.
  - `SetOsuData / SetDuckHunterData / SetInsertPiecesData / SetPendingScore` los llaman los controladores en `SetSpecificData` (ver `docs/gameplay.md`).
  - `SaveExerciseSummary(duration)`: `results.TryFinalize` (exige uso + misma duración; si no, warning y sin resultado) → construye `ExerciseSummary` → sugerencia híbrida si perfil/calibración coinciden, si no respaldo `GeneralSuggestionBuilder` (umbrales Insert 60/120 s, OSU 30/60 s, DuckHunter 0.2/0.5) o `No hay datos suficientes...` → `LastGeneralSuggestion` → construye `ExpositionSummary` → `SessionManager.CommitExerciseResult`.
  - `GeneralSuggestionBuilder` vive anidada en este archivo (no crear `.cs` nuevo sin reimportar).
- `SessionManager.cs` (singleton `DontDestroyOnLoad`): `CurrentSession`; `BeginSession` (id = `PeekNextSessionId`, solo con usuario y servicio listo); `CommitExerciseResult` delega y marca persistida si `Committed`; `EndSession` retorna solo si persistida; `ClearSession + DiscardPendingSession`.
- `SessionFinalizationOnLoad.cs` (escena `ScoresSummary`): `Awake → EndSession`, `OnDestroy → ClearSession`.
- `SessionReader.cs` (escena `SessionSummary`): lee `GetLastSessions(1)` (exige 3/3); header `Sesión #id, usuario, duración mm:ss, fecha es-ES`; tabs ejercicio + modos `Absolute/Relative` y `Usage/Exposition`; `RadarChart` (uso), `LineChart ×6` (historial 7 sesiones), `ExpositionPanel`; `generalSuggestionText = CurrentSummary.generalSuggestion` persistido.
- `ResultsUI.cs` (panel por ejercicio): `SetResults(duration, left, right)` pinta 4 `ArmResultUI`, textos abs/rel por zona, actividad/duración y `generalSuggestionText = SessionRecorder.LastGeneralSuggestion` estático.
- `ResultsManager.cs`: buffer `pendingDuration/Left/Right` (`OnTrackingStop → HandleSetResults`, `OnExcerciseStart → Clear`, `OnShowResults → ApplyPendingResults`); `Open/CloseResults` para el botón Métricas.

Secuencia por ejercicio: controlador `SetSpecificData` → `SetPendingScore` → `EndExercise` → `OnTrackingStop` (captura uso/exposición) → `OnExerciseFinalizing` (`SaveExerciseSummary`: une uso + desempeño + exposición + sugerencia y confirma transacción) → `OnShowResults` (pinta score + trofeo + métricas). La UI diferida (`ScoresSummary`, `SessionSummary`) lee los JSON ya confirmados.
