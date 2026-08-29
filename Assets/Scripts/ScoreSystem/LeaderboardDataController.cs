using System.Collections.Generic;
using UnityEngine;

public sealed class LeaderboardDataController : MonoBehaviour
{
    private const int TopEntryCount = 5;
    private const int DisplayRowCount = 6;

    [Header("Leaderboard")]
    [SerializeField] private ScoreExerciseType exerciseType = ScoreExerciseType.Insert;
    [SerializeField] private LeaderboardView view;
    [SerializeField] private ScoresSummaryController selectionSource;

    private UserService subscribedUserService;
    private ScoresSummaryController subscribedSelectionSource;

    private void Start()
    {
        SubscribeToExerciseSelection();
        SubscribeToUserChanges();
        Refresh();
    }

    private void OnDestroy()
    {
        if (subscribedSelectionSource != null)
            subscribedSelectionSource.OnExerciseSelected -= OnExerciseSelected;

        if (subscribedUserService != null)
            subscribedUserService.OnCurrentUserChanged -= OnCurrentUserChanged;
    }

    public void Refresh()
    {
        if (view == null)
        {
            Debug.LogWarning("[LeaderboardDataController] Falta la vista del leaderboard.");
            return;
        }

        PersistenceManager persistenceManager = PersistenceManager.Instance;
        if (persistenceManager == null || persistenceManager.LeaderboardService == null)
        {
            view.Render(new LeaderboardDisplayData(
                CreateHiddenRows(),
                "No se pudo cargar el leaderboard."));
            Debug.LogWarning("[LeaderboardDataController] LeaderboardService no disponible.");
            return;
        }

        SubscribeToUserChanges();

        UserData currentUser = persistenceManager.UserService?.CurrentUser;
        string userId = currentUser?.UserId;
        LeaderboardQueryResult result = persistenceManager.LeaderboardService.GetLeaderboard(
            exerciseType,
            userId,
            TopEntryCount);

        view.Render(BuildDisplayData(result));
    }

    private void SubscribeToUserChanges()
    {
        if (subscribedUserService != null)
            return;

        UserService userService = PersistenceManager.Instance?.UserService;
        if (userService == null)
            return;

        subscribedUserService = userService;
        subscribedUserService.OnCurrentUserChanged += OnCurrentUserChanged;
    }

    private void OnCurrentUserChanged()
    {
        Refresh();
    }

    private void SubscribeToExerciseSelection()
    {
        if (subscribedSelectionSource != null)
            return;

        ScoresSummaryController source = selectionSource != null
            ? selectionSource
            : GetComponent<ScoresSummaryController>();

        if (source == null)
        {
            Debug.LogWarning(
                "[LeaderboardDataController] No se encontró la fuente de selección de ejercicio.");
            return;
        }

        subscribedSelectionSource = source;
        subscribedSelectionSource.OnExerciseSelected += OnExerciseSelected;
    }

    private void OnExerciseSelected(ScoreExerciseType selectedExerciseType)
    {
        exerciseType = selectedExerciseType;
        Refresh();
    }

    private static LeaderboardDisplayData BuildDisplayData(LeaderboardQueryResult result)
    {
        List<LeaderboardRowData> rows = new List<LeaderboardRowData>(DisplayRowCount);
        IReadOnlyList<LeaderboardEntry> topEntries = result?.TopEntries;

        for (int i = 0; i < TopEntryCount; i++)
        {
            LeaderboardEntry entry = topEntries != null && i < topEntries.Count
                ? topEntries[i]
                : null;

            rows.Add(LeaderboardRowData.FromEntry(entry, i + 1, false));
        }

        bool showCurrentUserRow = result != null
            && result.HasCurrentUserEntry
            && result.CurrentUserPosition > TopEntryCount;

        rows.Add(showCurrentUserRow
            ? LeaderboardRowData.FromEntry(
                result.CurrentUserEntry,
                result.CurrentUserPosition,
                true)
            : LeaderboardRowData.Hidden());

        string statusMessage = BuildStatusMessage(result);
        return new LeaderboardDisplayData(rows, statusMessage);
    }

    private static string BuildStatusMessage(LeaderboardQueryResult result)
    {
        bool hasEntries = result != null
            && result.TopEntries != null
            && result.TopEntries.Count > 0;

        if (!hasEntries)
            return "Aún no hay puntuaciones registradas.";

        if (result == null || !result.HasCurrentUserEntry)
            return "Aún no tienes puntuación.";

        return string.Empty;
    }

    private static IReadOnlyList<LeaderboardRowData> CreateHiddenRows()
    {
        List<LeaderboardRowData> rows = new List<LeaderboardRowData>(DisplayRowCount);
        for (int i = 0; i < DisplayRowCount; i++)
            rows.Add(LeaderboardRowData.Hidden());

        return rows;
    }
}
