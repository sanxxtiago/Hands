using System;
using System.Collections;
using UnityEngine;

public class SlotBehaviour : MonoBehaviour
{
    public Transform snapPoint;
    public SlotType slotType;
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

        if (!piece.CanSnap(slotType, transform.position, snapDistance)) return;


        isSnapping = true;
        currentPiece = piece;

        StartCoroutine(AlignAndSnap(piece));
    }

    IEnumerator AlignAndSnap(PieceBehaviour piece)
    {
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

            //solo rota si el objeto lo necesita
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

        //asegurar posición final exacta
        piece.transform.position = snapPoint.position;

        if (piece.requireRotation)
        {
            piece.transform.rotation = targetRot;
        }
        else
        {
            piece.transform.rotation = Quaternion.identity;
        }
        piece.state = PieceState.Snapped;

        isSnapping = false;
        currentPiece = null;

        isFilled = true;
        piece.LockPhysics();
        piece.UpdateLayer();
        // Debug.Log(
        //  $"SNAPPED | isKinematic={piece.rb.isKinematic} | useGravity={piece.rb.useGravity}"
        // );

    }

}
