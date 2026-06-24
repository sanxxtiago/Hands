using System;
using UnityEngine;

public class OrientationSlotBehaviour : MonoBehaviour
{
    public event Action OnPieceEntered;
    public event Action OnPieceExited;
    public event Action OnPieceFitted;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<OrientationPieceBehaviour>(out _))
            return;

        OnPieceEntered?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent<OrientationPieceBehaviour>(out _))
            return;

        OnPieceExited?.Invoke();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.TryGetComponent<OrientationPieceBehaviour>(out var piece))
            return;

        if (!piece.isFitted && !piece.IsGrabbed)
        {
            piece.FitIn();
            OnPieceFitted?.Invoke();
        }
    }
}