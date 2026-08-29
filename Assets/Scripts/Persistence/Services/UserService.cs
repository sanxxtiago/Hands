using System;
using System.Collections.Generic;
using UnityEngine;

public class UserService
{
    private UsersData usersData = new();

    public event Action OnCurrentUserChanged;

    public UserData CurrentUser { get; private set; }
    public IReadOnlyList<UserProfile> Profiles => usersData.Profiles;
    public bool HasUsers => Profiles.Count > 0;
    public bool HasCurrentUser => CurrentUser != null;
    public string UserName => CurrentUser?.Name ?? string.Empty;
    public DateTime BirthDate => CurrentUser?.BirthDate ?? default;

    // Se conserva para no romper los consumidores actuales durante esta etapa.
    public bool IsRegistered => HasCurrentUser;

    public void Load()
    {
        usersData = SaveSystem.Load<UsersData>(SaveFiles.Users) ?? new UsersData();
        usersData.Profiles ??= new List<UserProfile>();

        CurrentUser = null;

        ActiveUserData activeUser = SaveSystem.Load<ActiveUserData>(SaveFiles.ActiveUser);
        UserProfile selectedProfile = FindProfile(activeUser?.UserId);

        if (selectedProfile == null && HasUsers)
        {
            selectedProfile = usersData.Profiles[0];
            SaveActiveUser(selectedProfile.UserId);
        }

        if (selectedProfile == null)
            return;

        CurrentUser = SaveSystem.Load<UserData>(
            selectedProfile.UserId,
            SaveFiles.User);

        if (CurrentUser == null)
        {
            Debug.LogWarning(
                $"[UserService] No se encontró user.json para el perfil {selectedProfile.UserId}.");
            return;
        }

        CurrentUser.UserId = selectedProfile.UserId;
    }

    public void Register(string name, DateTime birthDate)
    {
        CurrentUser = new UserData
        {
            UserId = Guid.NewGuid().ToString(),
            Name = name,
            BirthDate = birthDate
        };

        usersData.Profiles.Add(new UserProfile
        {
            UserId = CurrentUser.UserId,
            Name = CurrentUser.Name
        });

        SaveSystem.Save(CurrentUser.UserId, SaveFiles.User, CurrentUser);
        SaveSystem.Save(SaveFiles.Users, usersData);
        SaveActiveUser(CurrentUser.UserId);

        Debug.Log($"User registered: {CurrentUser}");

        PublishCurrentUserChanged();
    }

    public bool SelectUser(string userId)
    {
        UserProfile selectedProfile = FindProfile(userId);

        if (selectedProfile == null)
        {
            Debug.LogWarning($"[UserService] No existe un perfil con userId {userId}.");
            return false;
        }

        UserData selectedUser = SaveSystem.Load<UserData>(
            selectedProfile.UserId,
            SaveFiles.User);

        if (selectedUser == null)
        {
            Debug.LogWarning(
                $"[UserService] No se encontró user.json para el perfil {selectedProfile.UserId}.");
            return false;
        }

        selectedUser.UserId = selectedProfile.UserId;
        SaveActiveUser(selectedProfile.UserId);
        CurrentUser = selectedUser;

        PublishCurrentUserChanged();
        return true;
    }

    public void Save()
    {
        if (CurrentUser == null)
        {
            Debug.LogWarning("No hay un usuario para guardar.");
            return;
        }

        SaveSystem.Save(CurrentUser.UserId, SaveFiles.User, CurrentUser);
    }

    public void Delete()
    {
        if (CurrentUser == null)
            return;

        SaveSystem.Delete(CurrentUser.UserId, SaveFiles.User);
        CurrentUser = null;
        PublishCurrentUserChanged();
    }

    private UserProfile FindProfile(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        foreach (UserProfile profile in usersData.Profiles)
        {
            if (profile != null && profile.UserId == userId)
                return profile;
        }

        return null;
    }

    private void SaveActiveUser(string userId)
    {
        SaveSystem.Save(
            SaveFiles.ActiveUser,
            new ActiveUserData { UserId = userId });
    }

    private void PublishCurrentUserChanged()
    {
        string userDescription = CurrentUser == null
            ? "ninguno"
            : $"{CurrentUser.Name} ({CurrentUser.UserId})";

        Debug.Log(
            $"[UserService] OnCurrentUserChanged invocado. Usuario actual: {userDescription}.");

        OnCurrentUserChanged?.Invoke();
    }
}
