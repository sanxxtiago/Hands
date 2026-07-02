using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HunterExercise : ExerciseController
{
    [SerializeField] private HandPoseListener poseListener;

    protected override void OnExerciseStart()
    {
        throw new System.NotImplementedException();
    }

    private bool aiming;

    protected override void OnEnable()
    {
        base.OnEnable();
        poseListener.AimStarted += BeginAim;
        poseListener.AimEnded += EndAim;
        poseListener.ShootStarted += Shoot;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        poseListener.AimStarted -= BeginAim;
        poseListener.AimEnded -= EndAim;
        poseListener.ShootStarted -= Shoot;
    }

    private void BeginAim()
    {
        aiming = true;
    }

    private void EndAim()
    {
        aiming = false;
    }

    private void Shoot()
    {
        if (!aiming)
            return;

        Debug.Log("Bang!");
    }
}
