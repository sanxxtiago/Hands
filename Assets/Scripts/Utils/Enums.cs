using System;


public enum HAND
{
    LEFT = 1,
    RIGHT = 0
}
public enum GESTURESTATE
{
    IDLE,
    GRAB,
    PINCH,
    ROTATE
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
    DONUT = 3
}

public enum GAMESTATE
{
    IDLE,          // esperando
    COUNTDOWN,     // 3,2,1
    PLAYING,       // ejercicio activo
    RESULTS        // mostrar métricas
}


