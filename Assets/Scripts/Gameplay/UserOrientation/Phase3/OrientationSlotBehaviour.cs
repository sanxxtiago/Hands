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

    public OrientationPieceBehaviour CurrentPiece => currentPiece;
    public bool IsPieceInside => pieceInside;

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

        // Solo se encaja cuando la pieza ya descansa sobre la base (velocidad ~ 0),
        // permitiendo que caiga por gravedad y repose físicamente antes de validar.
        if (!piece.IsAtRest())
            return;

        CapturePiece();
    }

    public void CapturePiece()
    {
        if (hasFittedPiece || currentPiece == null || currentPiece.isFitted || currentPiece.IsGrabbed)
            return;

        hasFittedPiece = true;
        currentPiece.FitIn();
        OnPieceFitted?.Invoke();
    }
}
