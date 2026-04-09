using System;

public class SnackbarManager
{
    public static event Action<SNACKBARTYPE, string, float> OnShow;

    public static void Show(SNACKBARTYPE type, string message, float time)
    {
        OnShow?.Invoke(type, message, time);
    }
     public static void Show(SNACKBARTYPE type, string message)
    {
        OnShow?.Invoke(type, message, 2f);
    }
}
