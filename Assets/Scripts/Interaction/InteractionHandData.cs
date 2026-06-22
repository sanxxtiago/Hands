
using UnityEngine;

public struct InteractionHandData
{
    private readonly FrameMotionData frame;

    public HandType HandType => frame.handType;
    public long FrameId => frame.frameId;
    public float Timestamp => frame.timestamp;
    public Vector3 HandPos => frame.handPos;
    public InteractionHandData(FrameMotionData frame)
    {
        this.frame = frame;
    }

    // =========================
    // GESTURES
    // =========================

    public bool IsGestureActive(GestureType type)
    {
        foreach (var g in frame.gestures)
            if (g.type == type)
                return g.isActive;

        return false;
    }

    public float GetGestureStrength(GestureType type)
    {
        foreach (var g in frame.gestures)
            if (g.type == type)
                return g.strength;

        return 0f;
    }

    // =========================
    // MOTIONS
    // =========================

    public float GetMotionValue(MotionZone zone)
    {
        foreach (var m in frame.motions)
            if (m.zone == zone)
                return m.value;

        return 0f;
    }

    public float GetMotionVelocity(MotionZone zone)
    {
        foreach (var m in frame.motions)
            if (m.zone == zone)
                return m.velocity;

        return 0f;
    }

    public float GetMotionAngle(MotionZone zone)
    {
        foreach (var m in frame.motions)
            if (m.zone == zone)
                return m.rawAngle;

        return 0f;
    }

    public bool IsMotionActive(MotionZone zone)
    {
        foreach (var m in frame.motions)
            if (m.zone == zone)
                return m.isActive;

        return false;
    }
}