public interface IGestureDetector
{
    GestureState Evaluate(HandDataSnapshot snap);
}