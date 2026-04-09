using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SlotBehaviour : MonoBehaviour
{
    public float snapAngle = 20;
    public float snapDistance = 10;
    public float snapZOffset = -.02f;
    public Boolean isFilled = false;
    void OnTriggerStay(Collider other)
    {

        if (!other.TryGetComponent<PieceBehaviour>(out var piece)) return;

        if (CanSnap(piece))
        {
            Snap(piece);
        }
    }
    bool CanSnap(PieceBehaviour piece)
    {
        if (piece.isSnapped)
            return false;

        float dist = Vector3.Distance(piece.transform.position, transform.position);
        float angle = Quaternion.Angle(piece.transform.rotation, transform.rotation);

        return dist < snapDistance && angle < snapAngle;
    }

    void Snap(PieceBehaviour piece)
    {
        Vector3 offset = Vector3.forward * snapZOffset;
        piece.transform.SetPositionAndRotation(transform.position + offset, transform.rotation);
        piece.GetComponent<Rigidbody>().isKinematic = true;
        piece.isSnapped = true;
        isFilled = true;
    }
}
