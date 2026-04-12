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
        Debug.Log("Excersise Ended!!!!");
    }
    protected override void OnExerciseStart()
    {
        Debug.Log("Excersise Started!!!!");
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
