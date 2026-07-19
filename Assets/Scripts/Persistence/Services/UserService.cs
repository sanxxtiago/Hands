using System;
using UnityEngine;

public class UserService
{
    public UserData CurrentUser { get; private set; }
    public string UserName => CurrentUser?.Name ?? string.Empty;
    public DateTime BirthDate => CurrentUser?.BirthDate ?? default;

    public bool IsRegistered => CurrentUser != null;

    public void Load()
    {
        CurrentUser = SaveSystem.Load<UserData>(SaveFiles.User);
    }

    public void Register(string name, DateTime birthDate)
    {
        //Evita una sobreescritura del usuario local
        if (IsRegistered)
        {
            Debug.LogWarning("Ya existe un usuario registrado.");
            return;
        }

        CurrentUser = new UserData
        {
            Name = name,
            BirthDate = birthDate
        };

        Debug.Log($"User registered: {CurrentUser}");

        Save();
    }

    public void Save()
    {
        if (CurrentUser == null)
        {
            Debug.LogWarning("No hay un usuario para guardar.");
            return;
        }

        SaveSystem.Save(SaveFiles.User, CurrentUser);
    }

    public void Delete()
    {
        SaveSystem.Delete(SaveFiles.User);
        CurrentUser = null;
    }
}