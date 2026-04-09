using System;

public class SnackbarManager
{
    public static event Action<SNACKBARTYPE, string> OnShow;

    public static void Show(SNACKBARTYPE type, string message)
    {
        OnShow?.Invoke(type, message);
    }
}
