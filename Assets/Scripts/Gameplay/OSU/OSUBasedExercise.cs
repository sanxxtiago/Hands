public class OSUBasedExercise : ExerciseController
{
    public DotSpawner dotSpawner;

    protected override void OnExerciseStart()
    {
        dotSpawner.Spawn();
    
    }
    void Start()
    {

    }

    void Update()
    {

    }
}
