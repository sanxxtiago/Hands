

public enum HandType
{
    LEFT = 1,
    RIGHT = 0
}
public enum GestureType
{
    IDLE,
    GRAB,
    PINCH,
    ROTATE
}

public enum GesturePhase
{
    START,
    UPDATE,
    END
}

public enum InteractionType
{
    Grab,
    Select,
    Pinch,
    Rotate
}

public enum MotionZone
{
    Wrist,
    Forearm,
    Hand,
    WristFlexion,
    Supination,
    Pronation,
    HandElevation,
    Grab,
    Pinch
}
public enum ERRORTYPE
{
    CONNECTION,
    DETECTION
}

public enum TRACKINGSTATE
{
    NOTRACKING,
    TRACKING,
    ACTIVE
}


public enum SNACKBARTYPE
{
    WARNING,
    ERROR,
    SUCCESS
}

public enum INSERTTYPE
{
    SQUARE = 0,
    CIRCLE = 1,
    STAR = 2,
    DONUT = 3,
    Line,
    S,
    Z,
    L,
    Three
}

public enum GAMESTATE
{
    IDLE,          // esperando
    COUNTDOWN,     // 3,2,1
    PLAYING,       // ejercicio activo
    RESULTS        // mostrar métricas
}

public enum Exercise
{
    Insert,
    Osu
}


