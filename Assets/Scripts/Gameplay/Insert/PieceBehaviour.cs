public class PieceBehaviour : Grabbable
{
    public bool isSnapped;

    public override bool CanInteract(HAND hand)
    {
        return !isSnapped;
    }

    public void Snap()
    {
        isSnapped = true;
        rb.isKinematic = true;
    }
}
