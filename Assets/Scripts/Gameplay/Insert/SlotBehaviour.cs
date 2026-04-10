using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SlotBehaviour : MonoBehaviour
{
    public Transform snapPoint;
    public INSERTTYPE slotType;
    public float snapAngle = 20;
    public float snapDistance = 10;
    public float snapZOffset = -.02f;
    public float snapSpeed = 0.2f;
    public Boolean isFilled = false;
    private bool isSnapping = false;
    private PieceBehaviour currentPiece;
    void OnTriggerStay(Collider other)
    {
        if (isSnapping) return;

        if (!other.TryGetComponent<PieceBehaviour>(out var piece)) return;

        if (currentPiece != null) return;

        if (!CanSnap(piece)) return;

        isSnapping = true;
        currentPiece = piece;

        StartCoroutine(AlignAndSnap(piece));
    }
    bool CanSnap(PieceBehaviour piece)
    {

        if (piece.IsGrabbed || piece.isSnapped)
            return false;
        if (piece.pieceType != slotType)
            return false;

        float dist = Vector3.Distance(piece.transform.position, transform.position);

        return dist < snapDistance;
    }

    IEnumerator AlignAndSnap(PieceBehaviour piece)
    {
        if (piece.IsGrabbed)
            piece.ForceRelease();

        Collider col = piece.GetComponent<Collider>();
        Rigidbody rb = piece.GetComponent<Rigidbody>();

        col.isTrigger = true;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        Vector3 startPos = piece.transform.position;
        Quaternion startRot = piece.transform.rotation;

        float t = 0f;

        Quaternion targetRot = snapPoint.rotation;

        while (t < 1f)
        {
            //  t += Time.deltaTime * snapSpeed;
            t += Time.deltaTime * snapSpeed * (1f + t);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            piece.transform.position = Vector3.Lerp(startPos, snapPoint.position, smoothT);

            // 👇 solo rota si el objeto lo necesita
            if (piece.requireRotation)
            {
                piece.transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
            }
            else
            {
                piece.transform.rotation = Quaternion.identity;
            }

            yield return null;
        }

        // 👇 asegurar posición final exacta
        piece.transform.position = snapPoint.position;

        if (piece.requireRotation)
        {
            piece.transform.rotation = targetRot;
        }
        else
        {
            piece.transform.rotation = Quaternion.identity;
        }
        piece.isSnapped = true;

        isSnapping = false;
        currentPiece = null;

        isFilled = true;

        // 👇 opcional: dejarlo fijo pero con collider normal
        //col.isTrigger = false;
    }
    // void Snap(PieceBehaviour piece)
    // {
    //     if (piece.IsGrabbed)
    //     {
    //         piece.ForceRelease();
    //     }
    //     Vector3 offset = Vector3.forward * snapZOffset;
    //     piece.transform.SetPositionAndRotation(transform.position + offset, transform.rotation);
    //     piece.GetComponent<Rigidbody>().isKinematic = true;
    //     //Temporal
    //     piece.GetComponent<BoxCollider>().isTrigger = true;
    //     //----------
    //     piece.isSnapped = true;
    //     isFilled = true;
    // }
}
