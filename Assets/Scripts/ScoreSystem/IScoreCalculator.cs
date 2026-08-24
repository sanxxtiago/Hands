public interface IScoreCalculator<in TInput>
{
    ExerciseScore Calculate(TInput input);
}
