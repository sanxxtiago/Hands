public class PieceBehaviour : Grabbable
{
    public INSERTTYPE pieceType;
    public bool isSnapped;
    public bool requireRotation = false;

    public override bool CanInteract(HandType hand)
    {
        return !isSnapped;
    }

    public void Snap()
    {
        isSnapped = true;
        rb.isKinematic = true;
    }
}
