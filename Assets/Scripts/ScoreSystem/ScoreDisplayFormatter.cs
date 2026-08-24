using System.Globalization;
using System.Text;

public static class ScoreDisplayFormatter
{
    public static string FormatTotalScore(float score)
    {
        return score.ToString("0", CultureInfo.InvariantCulture) + "/100";
    }

    public static string FormatExerciseType(ScoreExerciseType exerciseType)
    {
        switch (exerciseType)
        {
            case ScoreExerciseType.OSU:
                return "OSU";
            case ScoreExerciseType.DuckHunter:
                return "DuckHunter";
            case ScoreExerciseType.Insert:
                return "Insert";
            default:
                return exerciseType.ToString();
        }
    }

    public static string FormatBreakdown(ScoreBreakdown[] breakdown)
    {
        if (breakdown == null || breakdown.Length == 0)
            return string.Empty;

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < breakdown.Length; i++)
        {
            if (i > 0)
                builder.AppendLine();

            ScoreBreakdown item = breakdown[i];
            builder.Append(FormatMetricId(item.metricId));
            builder.Append(": ");
            builder.Append(item.metricScore.ToString("0", CultureInfo.InvariantCulture));
            builder.Append("/100");
        }

        return builder.ToString();
    }

    private static string FormatMetricId(string metricId)
    {
        if (string.IsNullOrEmpty(metricId))
            return "Metric";

        string[] words = metricId.Split('_');
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < words.Length; i++)
        {
            if (i > 0)
                builder.Append(' ');

            string word = words[i];
            if (word.Length == 0)
                continue;

            builder.Append(char.ToUpperInvariant(word[0]));
            if (word.Length > 1)
                builder.Append(word.Substring(1));
        }

        return builder.ToString();
    }
}
