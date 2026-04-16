using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallInsertExercise : ExerciseController
{
    protected override bool IsCompleted()
    {
        return false;
    }
    protected override void OnExerciseEnd()
    {
#if UNITY_EDITOR
        Debug.Log("Excersise Ended!!!!");
#endif
    }
    protected override void OnExerciseStart()
    {
#if UNITY_EDITOR
        Debug.Log("Excersise Started!!!!");
#endif
    }

    protected override void Tick(float timeLeft)
    {
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
