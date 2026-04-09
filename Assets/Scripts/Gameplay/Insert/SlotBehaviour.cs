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
    public float snapSpeed = 0.2f;
    public Boolean isFilled = false;
    void OnTriggerStay(Collider other)
    {

        if (!other.TryGetComponent<PieceBehaviour>(out var piece)) return;

        if (CanSnap(piece))
        {
            //Snap(piece); //Directo
            StartCoroutine(AlignAndSnap(piece));
        }
    }
    bool CanSnap(PieceBehaviour piece)
    {
        if (piece.IsGrabbed || piece.isSnapped)
            return false;

        float dist = Vector3.Distance(piece.transform.position, transform.position);

        return dist < snapDistance;
    }
    IEnumerator AlignAndSnap(PieceBehaviour piece)
    {
        if (piece.IsGrabbed)
        {
            piece.ForceRelease();
        }
        piece.GetComponent<BoxCollider>().isTrigger = true;
        Rigidbody rb = piece.GetComponent<Rigidbody>();

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        Vector3 startPos = piece.transform.position;
        Quaternion startRot = piece.transform.rotation;

        float t = 0;
        Vector3 offset = Vector3.forward * snapZOffset;

        while (t < 1f)
        {
            t += Time.deltaTime * snapSpeed;

            piece.transform.position = Vector3.Lerp(startPos, transform.position + offset, t);
            piece.transform.rotation = Quaternion.Slerp(startRot, transform.rotation, t);

            yield return null;
        }

        piece.isSnapped = true;
        isFilled = true;

    }
    void Snap(PieceBehaviour piece)
    {
        if (piece.IsGrabbed)
        {
            piece.ForceRelease();
        }
        Vector3 offset = Vector3.forward * snapZOffset;
        piece.transform.SetPositionAndRotation(transform.position + offset, transform.rotation);
        piece.GetComponent<Rigidbody>().isKinematic = true;
        //Temporal
        piece.GetComponent<BoxCollider>().isTrigger = true;
        //----------
        piece.isSnapped = true;
        isFilled = true;
    }
}
