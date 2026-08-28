using System;
using UnityEngine;

public class OrientationSlotBehaviour : MonoBehaviour
{
    public event Action OnPieceEntered;
    public event Action OnPieceExited;
    public event Action OnPieceFitted;

    private bool pieceInside;
    private OrientationPieceBehaviour currentPiece;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<OrientationPieceBehaviour>(out var piece))
            return;

        if (piece.isFitted)
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

        if (piece.isFitted || piece.IsGrabbed)
            return;

        if (!pieceInside || piece != currentPiece)
            return;

        piece.FitIn();
        OnPieceFitted?.Invoke();
    }
}