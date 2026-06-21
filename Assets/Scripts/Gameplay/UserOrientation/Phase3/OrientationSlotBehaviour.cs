using System;
using UnityEngine;

public class OrientationSlotBehaviour : MonoBehaviour
{
    public event Action OnPieceEntered;
    public event Action OnPieceFitted;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerStay(Collider other)
    {
        OnPieceEntered?.Invoke();
        if (!other.TryGetComponent<OrientationPieceBehaviour>(out var piece)) return;
        if (!piece.isFitted && !piece.IsGrabbed)
        {
            piece.isFitted = true;
            OnPieceFitted?.Invoke();
        }

        Rigidbody rb = piece.GetComponent<Rigidbody>();

        rb.isKinematic = false;
    }
}
