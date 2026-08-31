using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public sealed class ExerciseResultPersistenceService
{
    private readonly SessionService sessionService;
    private readonly ScoreService scoreService;
    private readonly LeaderboardService leaderboardService;
    private readonly Dictionary<string, PendingExerciseResult> pendingResults =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, PendingExerciseResult> committedResults =
        new(StringComparer.Ordinal);

    private string userId;
    private string userName;
    private bool recoveryBlocked;

    public bool IsReady => !recoveryBlocked
        && sessionService != null
        && scoreService != null
        && leaderboardService != null
        && !string.IsNullOrWhiteSpace(userId);

    public ExerciseResultPersistenceService(
        SessionService sessionService,
        ScoreService scoreService,
        LeaderboardService leaderboardService,
        string userId,
        string userName)
    {
        this.sessionService = sessionService;
        this.scoreService = scoreService;
        this.leaderboardService = leaderboardService;
        SetUserContext(userId, userName);
    }

    public void SetUserContext(string userId, string userName)
    {
        this.userId = userId;
        this.userName = userName ?? string.Empty;
        pendingResults.Clear();
        committedResults.Clear();
        recoveryBlocked = false;
    }

    public bool RecoverPendingTransaction()
    {
        if (string.IsNullOrWhiteSpace(userId)
            || !SaveSystem.Exists(userId, SaveFiles.ExerciseCommit))
        {
            return true;
        }

        ExerciseResultTransaction transaction;
        try
        {
            transaction = SaveSystem.Load<ExerciseResultTransaction>(
                userId,
                SaveFiles.ExerciseCommit);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[ExerciseResultPersistence] No se pudo leer el journal: {exception.Message}");

            if (exception is JsonException)
            {
                DeletePendingJournal();
                return true;
            }

            recoveryBlocked = true;
            return false;
        }

        if (!IsValidTransaction(transaction)
            || !string.Equals(transaction.userId, userId, StringComparison.Ordinal))
        {
            Debug.LogWarning(
                "[ExerciseResultPersistence] Se descarto un journal invalido o de otro usuario.");
            DeletePendingJournal();
            return true;
        }

        bool applied;
        try
        {
            applied = ApplyTransaction(transaction);
        }
        catch (Exception exception)
        {
            recoveryBlocked = true;
            Debug.LogError(
                $"[ExerciseResultPersistence] Fallo durante la recuperacion: {exception.Message}");
            return false;
        }

        if (!applied)
        {
            recoveryBlocked = true;
            Debug.LogError(
                "[ExerciseResultPersistence] La recuperacion fallo; se bloquearan nuevos commits.");
            return false;
        }

        DeletePendingJournal();
        Debug.Log(
            $"[ExerciseResultPersistence] Journal recuperado: sessionGuid={transaction.sessionGuid}.");
        return true;
    }

    public ExerciseCommitOutcome CommitExerciseResult(
        SessionSummary session,
        ExerciseSummary summary,
        ExerciseScore score)
    {
        if (recoveryBlocked)
            return ExerciseCommitOutcome.Failed;

        if (!IsValidContext(session, summary, score))
            return ExerciseCommitOutcome.Rejected;

        string key = ExerciseResultIdentity.CreateKey(
            session.SessionGuid,
            summary.exerciseType);

        ExerciseSummary existingSummary = FindSummary(
            session,
            summary.exerciseType);

        if (pendingResults.TryGetValue(key, out PendingExerciseResult pendingResult))
        {
            if (!ExerciseResultIdentity.AreEquivalent(pendingResult.summary, summary)
                || !ExerciseResultIdentity.AreEquivalent(pendingResult.score, score))
            {
                Debug.LogWarning(
                    $"[ExerciseResultPersistence] Conflicto para la clave {key}.");
                return ExerciseCommitOutcome.Conflict;
            }

            if (existingSummary != null
                && (session.Summaries.Count < ExerciseResultIdentity.RequiredExerciseCount
                    || pendingResult.record.exerciseIndex != session.Summaries.Count - 1))
                return ExerciseCommitOutcome.Duplicate;

            return CompleteIfReady(session);
        }

        if (existingSummary != null)
        {
            if (committedResults.TryGetValue(key, out PendingExerciseResult committedResult)
                && (!ExerciseResultIdentity.AreEquivalent(committedResult.summary, summary)
                    || !ExerciseResultIdentity.AreEquivalent(committedResult.score, score)))
            {
                Debug.LogWarning(
                    $"[ExerciseResultPersistence] Conflicto para la clave {key}.");
                return ExerciseCommitOutcome.Conflict;
            }

            return ExerciseResultIdentity.AreEquivalent(existingSummary, summary)
                ? ExerciseCommitOutcome.Duplicate
                : ExerciseCommitOutcome.Conflict;
        }

        session.AddSummary(summary);

        ScoreRecord record = CreateScoreRecord(
            session,
            summary.exerciseType,
            score,
            session.Summaries.Count - 1);

        pendingResults.Add(
            key,
            new PendingExerciseResult(summary, score, record));

        ExerciseCommitOutcome outcome = CompleteIfReady(session);
        if (outcome == ExerciseCommitOutcome.Rejected)
        {
            pendingResults.Remove(key);
            session.Summaries.Remove(summary);
        }

        return outcome;
    }

    public void DiscardPendingSession(string sessionGuid)
    {
        if (string.IsNullOrWhiteSpace(sessionGuid))
            return;

        List<string> keysToRemove = new();
        foreach (KeyValuePair<string, PendingExerciseResult> pair in pendingResults)
        {
            if (pair.Value.record.sessionGuid == sessionGuid)
                keysToRemove.Add(pair.Key);
        }

        foreach (KeyValuePair<string, PendingExerciseResult> pair in committedResults)
        {
            if (pair.Value.record.sessionGuid == sessionGuid
                && !keysToRemove.Contains(pair.Key))
            {
                keysToRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < keysToRemove.Count; i++)
        {
            pendingResults.Remove(keysToRemove[i]);
            committedResults.Remove(keysToRemove[i]);
        }
    }

    private ExerciseCommitOutcome CompleteIfReady(SessionSummary session)
    {
        int expectedCount = session.Summaries.Count;
        if (expectedCount < ExerciseResultIdentity.RequiredExerciseCount)
            return ExerciseCommitOutcome.Pending;

        if (!ExerciseResultIdentity.IsCompleteSession(session))
            return ExerciseCommitOutcome.Rejected;

        ExerciseResultTransaction transaction = BuildTransaction(session);
        if (transaction == null)
            return ExerciseCommitOutcome.Rejected;

        try
        {
            SaveSystem.Save(userId, SaveFiles.ExerciseCommit, transaction);

            if (!ApplyTransaction(transaction))
                return ExerciseCommitOutcome.Failed;

            for (int i = 0; i < transaction.items.Count; i++)
            {
                ExerciseResultTransactionItem item = transaction.items[i];
                if (pendingResults.TryGetValue(item.idempotencyKey, out PendingExerciseResult result))
                    committedResults[item.idempotencyKey] = result;
            }
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[ExerciseResultPersistence] Fallo al confirmar el resultado: {exception.Message}");
            return ExerciseCommitOutcome.Failed;
        }

        DeletePendingJournal();
        pendingResults.Clear();
        return ExerciseCommitOutcome.Committed;
    }

    private ExerciseResultTransaction BuildTransaction(SessionSummary session)
    {
        if (!ExerciseResultIdentity.IsCompleteSession(session)
            || string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        ExerciseResultTransaction transaction = new()
        {
            userId = userId,
            userName = userName,
            sessionGuid = session.SessionGuid,
            sessionId = session.SessionId,
            session = session
        };

        for (int i = 0; i < session.Summaries.Count; i++)
        {
            ExerciseSummary summary = session.Summaries[i];
            string key = ExerciseResultIdentity.CreateKey(
                session.SessionGuid,
                summary.exerciseType);

            if (!pendingResults.TryGetValue(key, out PendingExerciseResult pendingResult))
                return null;

            transaction.items.Add(new ExerciseResultTransactionItem
            {
                idempotencyKey = key,
                exerciseType = summary.exerciseType,
                summary = summary,
                score = pendingResult.record
            });
        }

        return IsValidTransaction(transaction) ? transaction : null;
    }

    private bool ApplyTransaction(ExerciseResultTransaction transaction)
    {
        if (sessionService == null
            || scoreService == null
            || leaderboardService == null)
        {
            return false;
        }

        if (!sessionService.UpsertSession(transaction.session))
        {
            Debug.LogError(
                "[ExerciseResultPersistence] No se pudo insertar o validar la sesion.");
            return false;
        }

        for (int i = 0; i < transaction.items.Count; i++)
        {
            if (!scoreService.UpsertScore(transaction.items[i].score))
            {
                Debug.LogError(
                    "[ExerciseResultPersistence] No se pudo insertar o validar un score.");
                return false;
            }
        }

        for (int i = 0; i < transaction.items.Count; i++)
        {
            ExerciseResultTransactionItem item = transaction.items[i];
            leaderboardService.UpdateHighscore(
                item.score,
                transaction.userId,
                transaction.userName);
        }

        return true;
    }

    private bool IsValidContext(
        SessionSummary session,
        ExerciseSummary summary,
        ExerciseScore score)
    {
        if (string.IsNullOrWhiteSpace(userId)
            || session == null
            || summary == null
            || score == null
            || !score.isValid
            || !ScoreMath.IsFinite(score.totalScore)
            || score.totalScore < 0f
            || string.IsNullOrWhiteSpace(session.SessionGuid)
            || session.SessionId <= 0)
        {
            return false;
        }

        if (!ExerciseResultIdentity.TryGetExerciseType(
                score.exerciseType,
                out ExerciseType scoreExerciseType)
            || scoreExerciseType != summary.exerciseType)
        {
            return false;
        }

        if (session.Summaries == null
            || session.Summaries.Count > ExerciseResultIdentity.RequiredExerciseCount
            || (session.Summaries.Count == ExerciseResultIdentity.RequiredExerciseCount
                && FindSummary(session, summary.exerciseType) == null))
        {
            return false;
        }

        return true;
    }

    private bool IsValidTransaction(ExerciseResultTransaction transaction)
    {
        if (transaction == null
            || string.IsNullOrWhiteSpace(transaction.userId)
            || string.IsNullOrWhiteSpace(transaction.sessionGuid)
            || transaction.sessionId <= 0
            || transaction.session == null
            || transaction.sessionGuid != transaction.session.SessionGuid
            || transaction.sessionId != transaction.session.SessionId
            || !ExerciseResultIdentity.IsCompleteSession(transaction.session)
            || transaction.items == null
            || transaction.items.Count != ExerciseResultIdentity.RequiredExerciseCount)
        {
            return false;
        }

        HashSet<ExerciseType> exerciseTypes = new();
        HashSet<int> exerciseIndexes = new();
        for (int i = 0; i < transaction.items.Count; i++)
        {
            ExerciseResultTransactionItem item = transaction.items[i];
            ExerciseSummary sessionSummary = FindSummary(
                transaction.session,
                item?.exerciseType ?? default);
            if (item == null
                || item.summary == null
                || item.score == null
                || !item.score.isValid
                || item.score.recordedAt == default
                || !ScoreMath.IsFinite(item.score.totalScore)
                || item.score.totalScore < 0f
                || !exerciseTypes.Add(item.exerciseType)
                || !exerciseIndexes.Add(item.score.exerciseIndex)
                || item.score.exerciseIndex < 0
                || item.score.exerciseIndex >= ExerciseResultIdentity.RequiredExerciseCount
                || !ExerciseResultIdentity.AreEquivalent(sessionSummary, item.summary)
                || item.summary.exerciseType != item.exerciseType
                || item.idempotencyKey != ExerciseResultIdentity.CreateKey(
                    transaction.sessionGuid,
                    item.exerciseType)
                || item.score.sessionGuid != transaction.sessionGuid
                || item.score.sessionIdNumeric != transaction.sessionId
                || !ExerciseResultIdentity.TryGetExerciseType(
                    item.score.exerciseType,
                    out ExerciseType scoreExerciseType)
                || scoreExerciseType != item.exerciseType)
            {
                return false;
            }
        }

        return exerciseTypes.Contains(ExerciseType.Insert)
            && exerciseTypes.Contains(ExerciseType.OSU)
            && exerciseTypes.Contains(ExerciseType.DuckHunter);
    }

    private ScoreRecord CreateScoreRecord(
        SessionSummary session,
        ExerciseType exerciseType,
        ExerciseScore score,
        int exerciseIndex)
    {
        ExerciseResultIdentity.TryGetScoreExerciseType(
            exerciseType,
            out ScoreExerciseType scoreExerciseType);

        return new ScoreRecord
        {
            sessionGuid = session.SessionGuid,
            sessionIdNumeric = session.SessionId,
            exerciseIndex = exerciseIndex,
            exerciseType = scoreExerciseType,
            recordedAt = DateTime.Now,
            totalScore = score.totalScore,
            scoreGrade = score.scoreGrade,
            trophyTier = score.trophyTier,
            classificationProfileVersion = score.classificationProfileVersion,
            motivationalMessage = score.motivationalMessage,
            isValid = score.isValid,
            statsData = score.statsData,
            breakdown = score.breakdown == null
                ? Array.Empty<ScoreBreakdown>()
                : (ScoreBreakdown[])score.breakdown.Clone()
        };
    }

    private static ExerciseSummary FindSummary(
        SessionSummary session,
        ExerciseType exerciseType)
    {
        if (session?.Summaries == null)
            return null;

        for (int i = 0; i < session.Summaries.Count; i++)
        {
            ExerciseSummary summary = session.Summaries[i];
            if (summary != null && summary.exerciseType == exerciseType)
                return summary;
        }

        return null;
    }

    private void DeletePendingJournal()
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        try
        {
            SaveSystem.Delete(userId, SaveFiles.ExerciseCommit);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[ExerciseResultPersistence] No se pudo eliminar el journal: {exception.Message}");
        }
    }

    private sealed class PendingExerciseResult
    {
        public readonly ExerciseSummary summary;
        public readonly ExerciseScore score;
        public readonly ScoreRecord record;

        public PendingExerciseResult(
            ExerciseSummary summary,
            ExerciseScore score,
            ScoreRecord record)
        {
            this.summary = summary;
            this.score = score;
            this.record = record;
        }
    }
}
