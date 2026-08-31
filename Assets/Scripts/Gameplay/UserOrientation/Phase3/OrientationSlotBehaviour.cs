using System;
using UnityEngine;

public class OrientationSlotBehaviour : MonoBehaviour
{
    public event Action OnPieceEntered;
    public event Action OnPieceExited;
    public event Action OnPieceFitted;

    private bool pieceInside;
    private OrientationPieceBehaviour currentPiece;
    private bool hasFittedPiece;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<OrientationPieceBehaviour>(out var piece))
            return;

        if (piece.isFitted || hasFittedPiece)
            return;

        pieceInside = true;
        currentPiece = piece;
        OnPieceEntered?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent<OrientationPieceBehaviour>(out var piece))
            return;

        if (piece != currentPiece)
            return;

        pieceInside = false;
        currentPiece = null;
        OnPieceExited?.Invoke();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.TryGetComponent<OrientationPieceBehaviour>(out var piece))
            return;

        if (piece.isFitted || piece.IsGrabbed || hasFittedPiece)
            return;

        if (!pieceInside || piece != currentPiece)
            return;

        hasFittedPiece = true;
        piece.FitIn();
        OnPieceFitted?.Invoke();
    }
}
