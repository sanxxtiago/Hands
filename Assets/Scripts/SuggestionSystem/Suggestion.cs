public enum SuggestionType
{
    Zone,
    LowActivity
}

public class Suggestion
{
    public string message;
    public float severity;
    public SuggestionType type;
}