using UnityEngine;

[CreateAssetMenu(
    fileName = "ScoreClassificationCatalog",
    menuName = "Score System/Score Classification Catalog")]
public sealed class ScoreClassificationCatalog : ScriptableObject
{
    [SerializeField] private ScoreClassificationProfile insertProfile;
    [SerializeField] private ScoreClassificationProfile osuProfile;
    [SerializeField] private ScoreClassificationProfile duckHunterProfile;

    public ScoreClassificationProfile GetProfile(ScoreExerciseType exerciseType)
    {
        switch (exerciseType)
        {
            case ScoreExerciseType.Insert:
                return insertProfile;
            case ScoreExerciseType.OSU:
                return osuProfile;
            case ScoreExerciseType.DuckHunter:
                return duckHunterProfile;
            default:
                return null;
        }
    }

    public bool TryValidate(out string validationError)
    {
        if (!ValidateProfile(ScoreExerciseType.Insert, insertProfile, out validationError))
            return false;

        if (!ValidateProfile(ScoreExerciseType.OSU, osuProfile, out validationError))
            return false;

        if (!ValidateProfile(
            ScoreExerciseType.DuckHunter,
            duckHunterProfile,
            out validationError))
        {
            return false;
        }

        validationError = null;
        return true;
    }

    private static bool ValidateProfile(
        ScoreExerciseType exerciseType,
        ScoreClassificationProfile profile,
        out string validationError)
    {
        if (profile == null)
        {
            validationError = $"falta el perfil para {exerciseType}";
            return false;
        }

        if (!profile.TryValidate(out string profileError))
        {
            validationError = $"perfil de {exerciseType}: {profileError}";
            return false;
        }

        validationError = null;
        return true;
    }

    private void OnValidate()
    {
        if (!TryValidate(out string validationError))
        {
            Debug.LogError(
                $"[ScoreSystem] Catalogo de clasificacion invalido: {validationError}.",
                this);
        }
    }
}
