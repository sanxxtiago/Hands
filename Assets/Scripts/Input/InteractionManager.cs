using System.Collections.Generic;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public GestureDetector detector;
    public float grabRadius = 0.1f;
    public LayerMask grabbableLayer;

    [Header("Smoothing")]
    public float positionSmooth = 20f;
    public float rotationSmooth = 15f;
    public float handSmooth = 20f;

    private Dictionary<HAND, GameObject> grabbedObjects = new();

    private Dictionary<HAND, Vector3> handPositions = new();
    private Dictionary<HAND, Quaternion> handRotations = new();

    private Dictionary<HAND, Vector3> positionOffsets = new();
    private Dictionary<HAND, Quaternion> rotationOffsets = new();

    private Dictionary<HAND, Vector3> handVelocities = new();
    private Dictionary<HAND, Vector3> smoothHandPositions = new();

    void OnEnable()
    {
        detector.OnGrabStart += HandleGrabStart;
        detector.OnGrabEnd += HandleGrabEnd;
        detector.OnHandUpdate += HandleHandUpdate;
    }

    void OnDisable()
    {
        detector.OnGrabStart -= HandleGrabStart;
        detector.OnGrabEnd -= HandleGrabEnd;
        detector.OnHandUpdate -= HandleHandUpdate;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        foreach (var pair in grabbedObjects)
        {
            HAND hand = pair.Key;
            GameObject obj = pair.Value;

            if (!handPositions.ContainsKey(hand) || !handRotations.ContainsKey(hand))
                continue;

            //targets con offset
            Vector3 targetPos = handPositions[hand] + positionOffsets[hand];
            Quaternion targetRot = handRotations[hand] * rotationOffsets[hand];

            // // 🧈 suavizado
            // obj.transform.position = Vector3.Lerp(
            //     obj.transform.position,
            //     targetPos,
            //     positionSmooth * dt
            // );

            // obj.transform.rotation = Quaternion.Slerp(
            //     obj.transform.rotation,
            //     targetRot,
            //     rotationSmooth * dt
            // );
            Rigidbody rb = obj.GetComponent<Rigidbody>();

            Vector3 velocity = (targetPos - rb.position) * positionSmooth;
            rb.velocity = velocity;

            Quaternion delta = targetRot * Quaternion.Inverse(rb.rotation);
            delta.ToAngleAxis(out float angle, out Vector3 axis);

            if (angle > 180f) angle -= 360f;

            Vector3 angularVelocity = angle * Mathf.Deg2Rad * rotationSmooth * axis;

            rb.angularVelocity = angularVelocity;
        }
    }

    void HandleGrabStart(GestureInputEventArgs e)
    {
        if (grabbedObjects.ContainsKey(e.hand))
            return;

        Collider[] hits = Physics.OverlapSphere(e.handPosition, grabRadius, grabbableLayer);
        if (hits.Length == 0) return;

        //closest
        GameObject closest = hits[0].gameObject;
        float minDist = Vector3.Distance(e.handPosition, closest.transform.position);

        Rigidbody rb = closest.GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.drag = 10f;
        rb.angularDrag = 10f;

        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(e.handPosition, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = hit.gameObject;
            }
        }

        grabbedObjects[e.hand] = closest;

        //offsets
        positionOffsets[e.hand] = closest.transform.position - e.handPosition;

        if (handRotations.ContainsKey(e.hand))
        {
            rotationOffsets[e.hand] =
                Quaternion.Inverse(handRotations[e.hand]) * closest.transform.rotation;
        }
        else
        {
            rotationOffsets[e.hand] = Quaternion.identity;
        }
    }

    void HandleGrabEnd(GestureInputEventArgs e)
    {
        if (!grabbedObjects.ContainsKey(e.hand))
            return;

        GameObject obj = grabbedObjects[e.hand];
        Rigidbody rb = obj.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // Restaurar física
            //rb.useGravity = true;
            rb.drag = 0f;
            rb.angularDrag = 0.05f;

            rb.velocity = handVelocities[e.hand];
        }

        //Se limpian los diccionarios
        grabbedObjects.Remove(e.hand);
        positionOffsets.Remove(e.hand);
        rotationOffsets.Remove(e.hand);

    }

    void HandleHandUpdate(GestureInputEventArgs e)
    {
        if (!smoothHandPositions.ContainsKey(e.hand))
            smoothHandPositions[e.hand] = e.handPosition;

        smoothHandPositions[e.hand] = Vector3.Lerp(
            smoothHandPositions[e.hand],
            e.handPosition,
            Time.deltaTime * handSmooth
        );
        
        handPositions[e.hand] = e.handPosition;
        handRotations[e.hand] = e.handRotation;
        Vector3 velocity = (e.handPosition - handPositions[e.hand]) / Time.deltaTime;
        handVelocities[e.hand] = velocity;
    }
}