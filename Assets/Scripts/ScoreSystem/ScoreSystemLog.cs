using System;

internal static class ScoreSystemLog
{
    public static void Warning(string message)
    {
        Console.WriteLine("[ScoreSystem] Warning: " + message);
    }

    public static void Error(string message)
    {
        Console.WriteLine("[ScoreSystem] Error: " + message);
    }
}
