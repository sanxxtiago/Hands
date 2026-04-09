using System;
using Leap;
using UnityEngine;

public class HandDetector : MonoBehaviour
{
    public static event Action OnHandsLost;
    public static event Action OnHandsDetected;

    public LeapProvider leapProvider;
    private float noHandsTimer = 0f;
    public float delay = 0.5f;
    private bool hadHands = false;

    void Update()
    {
        bool hasHands = leapProvider.CurrentFrame.Hands.Count > 0;

        if (hasHands)
        {
            noHandsTimer = 0f;

            if (!hadHands)
            {
                //OnHandsDetected?.Invoke();
                SnackbarManager.Show(SNACKBARTYPE.SUCCESS, "Manos encontradas!");
            }
        }
        else
        {
            noHandsTimer += Time.deltaTime;

            if (hadHands)// && noHandsTimer >= delay)
            {
                //OnHandsLost?.Invoke();
                SnackbarManager.Show(SNACKBARTYPE.ERROR, "Manos fuera del área de seguimiento!");

            }
        }

        hadHands = hasHands;
    }
}
