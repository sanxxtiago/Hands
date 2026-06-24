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
        //Debug.Log($"FROM PF: FRAME:{frame.handType} & THIS.HT:{handType}");

        var interactionHandData = new InteractionHandData(frame);

        HandleGrab(interactionHandData);
        HandlePinch(interactionHandData);
        HandleRotation(interactionHandData);
    }

    // =========================
    // GRAB
    // =========================
    void HandleGrab(InteractionHandData interactionHandData)
    {
        bool currentGrab = interactionHandData.IsGestureActive(GestureType.GRAB);
        float strength = interactionHandData.GetGestureStrength(GestureType.GRAB);

        if (!isGrabbing && currentGrab)
            Emit(interactionHandData, GestureType.GRAB, GesturePhase.START, strength);

        if (isGrabbing && currentGrab)
            Emit(interactionHandData, GestureType.GRAB, GesturePhase.UPDATE, strength);

        if (isGrabbing && !currentGrab)
            Emit(interactionHandData, GestureType.GRAB, GesturePhase.END, strength);

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
    void HandleRotation(InteractionHandData interactionHandData)
    {

        //bool rotateGesture = interactionHandData.IsGestureActive(GestureType.ROTATE);
        bool rotateGesture = interactionHandData.IsMotionActive(MotionZone.Wrist);
        //float strength = interactionHandData.GetGestureStrength(GestureType.ROTATE);

        float strength = interactionHandData.GetMotionValue(MotionZone.Wrist);
        //se puede hacer detección de rotación solo al agarrar
        //bool currentRotate = isGrabbing && rotateGesture;

        //se puede superponer rotación con agarre y pinch  
        bool currentRotate = rotateGesture;


        if (!isRotating && currentRotate)
            Emit(interactionHandData, GestureType.ROTATE, GesturePhase.START, strength);

        if (isRotating && currentRotate)
            Emit(interactionHandData, GestureType.ROTATE, GesturePhase.UPDATE, strength);

        if (isRotating && !currentRotate)
            Emit(interactionHandData, GestureType.ROTATE, GesturePhase.END, strength);

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
            palmPosition = hand.PalmPos,
            palmRotation = hand.PalmRotation,
            strength = strength,
            frameId = hand.FrameId

            //Se puede inyectar poseDetector
        };
        //Debug.Log($"TYPE: {e.type} - PHASE: {e.phase}");

        OnInteraction?.Invoke(e);
    }
}