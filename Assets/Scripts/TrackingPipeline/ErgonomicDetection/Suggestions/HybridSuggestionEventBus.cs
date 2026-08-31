using System;

public static class HybridSuggestionEventBus
{
    public static event Action<HybridSuggestionData> OnSuggestion;

    public static void Publish(HybridSuggestionData suggestion)
    {
        OnSuggestion?.Invoke(suggestion);
    }
}
