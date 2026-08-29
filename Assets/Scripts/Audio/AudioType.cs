public enum AudioType
{
    None = 0,

    // One-shots generales
    ButtonClick = 1,
    CountdownTick = 2,
    CountdownFinished = 3,
    ExerciseStart = 4,
    ExerciseCompleted = 5,
    Success = 6,
    Warning = 7,
    Error = 8,

    // Sonidos de ejercicios (no reordenar; añadir siempre al final)
    OsuHaloTimer = 9,
    LaserShot = 10,
    PieceSnapped = 11,
    PhaseCompleted = 12,
    ExerciseAmbience = 13,
    MenuTheme = 14,
    DuckHit = 15,
    TrophyReveal = 16,
    OsuTargetHit = 17,
    OsuTargetFailed = 18,
    PieceGrabbed = 19,
    DuckEscape = 20,
    TrophyGold = 21,
    TrophySilver = 22,
    TrophyBronze = 23
}
