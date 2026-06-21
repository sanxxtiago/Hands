using System;
using UnityEngine;

public class InteractionTracker : IDisposable
{
    public event Action<InteractionEvent> OnInteraction;

    private readonly HandType handType;

    private bool isGrabbing;
    private bool isRotating;
    private bool isPinching;

    public InteractionTracker(HandType handType)
    {
        this.handType = handType;
        MotionEventBus.OnFrame += ProcessFrame;
    }

    public void Dispose()
    {
        MotionEventBus.OnFrame -= ProcessFrame;
    }

    private void ProcessFrame(FrameMotionData frame)
    {
        if (frame.handType != handType)
            return;

        var hand = new InteractionHandData(frame);

        HandleGrab(hand);
        HandlePinch(hand); 
        HandleRotation(hand);
    }

    // =========================
    // GRAB
    // =========================
    void HandleGrab(InteractionHandData hand)
    {
        bool currentGrab = hand.IsGestureActive(GestureType.GRAB);
        float strength = hand.GetGestureStrength(GestureType.GRAB);

        if (!isGrabbing && currentGrab)
            Emit(hand, GestureType.GRAB, GesturePhase.START, strength);

        if (isGrabbing && currentGrab)
            Emit(hand, GestureType.GRAB, GesturePhase.UPDATE, strength);

        if (isGrabbing && !currentGrab)
            Emit(hand, GestureType.GRAB, GesturePhase.END, strength);

        isGrabbing = currentGrab;
    }

    // =========================
    // PINCH
    // =========================
    void HandlePinch(InteractionHandData hand)
    {
        bool currentPinch = hand.IsGestureActive(GestureType.PINCH);
        float strength = hand.GetGestureStrength(GestureType.PINCH);

        // Opcional para descartar pinch anteriores
        if (isGrabbing)
            currentPinch = false;

        if (!isPinching && currentPinch)
            Emit(hand, GestureType.PINCH, GesturePhase.START, strength);

        if (isPinching && currentPinch)
            Emit(hand, GestureType.PINCH, GesturePhase.UPDATE, strength);

        if (isPinching && !currentPinch)
            Emit(hand, GestureType.PINCH, GesturePhase.END, strength);

        isPinching = currentPinch;
    }
    // =========================
    // ROTATION
    // =========================
    void HandleRotation(InteractionHandData hand)
    {
        //se puede combinar gesture + motion 
        bool rotateGesture = hand.IsGestureActive(GestureType.ROTATE);
        float strength = hand.GetGestureStrength(GestureType.ROTATE);

        bool currentRotate = isGrabbing && rotateGesture;

        if (!isRotating && currentRotate)
            Emit(hand, GestureType.ROTATE, GesturePhase.START, strength);

        if (isRotating && currentRotate)
            Emit(hand, GestureType.ROTATE, GesturePhase.UPDATE, strength);

        if (isRotating && !currentRotate)
            Emit(hand, GestureType.ROTATE, GesturePhase.END, strength);

        isRotating = currentRotate;
    }

    // =========================
    // EMIT
    // =========================
    void Emit(InteractionHandData hand, GestureType type, GesturePhase phase, float strength)
    {
        InteractionEvent e = new InteractionEvent
        {
            type = type,
            phase = phase,
            handType = hand.HandType,
            strength = strength,
            frameId = hand.FrameId

            //Se puede inyectar poseDetector
        };

        OnInteraction?.Invoke(e);
    }
}