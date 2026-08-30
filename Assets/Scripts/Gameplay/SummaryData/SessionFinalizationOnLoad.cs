using UnityEngine;

public sealed class SessionFinalizationOnLoad : MonoBehaviour
{
    private void Awake()
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogWarning("[SessionFinalization] No existe un SessionManager al iniciar ScoresSummary.");
            return;
        }

        SessionManager.Instance.EndSession();
    }

    private void OnDestroy()
    {
        if (SessionManager.Instance != null)
            SessionManager.Instance.ClearSession();
    }
}
