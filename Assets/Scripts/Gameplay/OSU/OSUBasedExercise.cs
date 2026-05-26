public class OSUBasedExercise : ExerciseController
{
    public DotSpawner dotSpawner;
    protected override bool IsCompleted()
    {
        return false;
    }

    protected override void OnExerciseStart()
    {
        dotSpawner.Spawn();
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
