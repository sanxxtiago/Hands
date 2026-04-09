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
    PINCH
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
