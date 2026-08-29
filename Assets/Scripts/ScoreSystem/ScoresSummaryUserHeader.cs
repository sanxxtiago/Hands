using TMPro;
using UnityEngine;

public sealed class ScoresSummaryUserHeader : MonoBehaviour
{
    [SerializeField] private TMP_Text userNameText;

    private UserService subscribedUserService;

    private void OnEnable()
    {
        SubscribeToUserService();
        RefreshUserName();
    }

    private void OnDisable()
    {
        UnsubscribeFromUserService();
    }

    private void SubscribeToUserService()
    {
        if (subscribedUserService != null || PersistenceManager.Instance == null)
            return;

        UserService userService = PersistenceManager.Instance.UserService;
        if (userService == null)
            return;

        userService.OnCurrentUserChanged += RefreshUserName;
        subscribedUserService = userService;
    }

    private void UnsubscribeFromUserService()
    {
        if (subscribedUserService == null)
            return;

        subscribedUserService.OnCurrentUserChanged -= RefreshUserName;
        subscribedUserService = null;
    }

    private void RefreshUserName()
    {
        if (userNameText == null)
            return;

        userNameText.text = PersistenceManager.Instance != null &&
            PersistenceManager.Instance.UserService != null
            ? PersistenceManager.Instance.UserService.UserName
            : string.Empty;
    }
}
