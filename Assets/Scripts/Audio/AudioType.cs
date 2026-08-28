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

    // Trofeo
    TrophyGold = 16,
    TrophySilver = 17,
    TrophyBronze = 18
}
