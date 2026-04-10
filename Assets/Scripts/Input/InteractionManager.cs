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

    private Dictionary<HAND, Interactable> grabbedObjects = new();

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
            Interactable interactable = pair.Value;

            if (!handPositions.ContainsKey(hand) || !handRotations.ContainsKey(hand))
                continue;

            Vector3 targetPos = handPositions[hand] + positionOffsets[hand];
            Quaternion targetRot = handRotations[hand] * rotationOffsets[hand];

            interactable.OnGrabUpdate(
                hand,
                targetPos,
                targetRot,
                positionSmooth,
                rotationSmooth
            );
        }
    }

    void HandleGrabStart(GestureInputEventArgs e)
    {
        if (grabbedObjects.ContainsKey(e.hand))
            return;

        Collider[] hits = Physics.OverlapSphere(e.handPosition, grabRadius, grabbableLayer);
        if (hits.Length == 0) return;

        GameObject closest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(e.handPosition, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = hit.gameObject;
            }
        }

        Interactable interactable = closest.GetComponent<Interactable>();

        if (interactable == null || !interactable.CanInteract(e.hand))
            return;

        grabbedObjects[e.hand] = interactable;

        interactable.OnGrabStart(e.hand, e.handPosition, e.handRotation);
        interactable.OnForcedRelease += HandleForcedRelease;

        // offsets (siguen igual)
        positionOffsets[e.hand] = closest.transform.position - e.handPosition;

        rotationOffsets[e.hand] =
            Quaternion.Inverse(e.handRotation) * closest.transform.rotation;
    }

    void HandleGrabEnd(GestureInputEventArgs e)
    {
        if (!grabbedObjects.ContainsKey(e.hand))
            return;

        Interactable interactable = grabbedObjects[e.hand];

        interactable.OnGrabEnd(e.hand, handVelocities[e.hand]);
        interactable.OnForcedRelease -= HandleForcedRelease;

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

        handRotations[e.hand] = e.handRotation;
        if (handPositions.ContainsKey(e.hand))
        {
            Vector3 velocity = (e.handPosition - handPositions[e.hand]) / Time.deltaTime;
            handVelocities[e.hand] = velocity;
        }

        handPositions[e.hand] = e.handPosition;
    }

    void HandleForcedRelease(Interactable target)
    {
        HAND? handToRemove = null;

        foreach (var pair in grabbedObjects)
        {
            if (pair.Value == target)
            {
                handToRemove = pair.Key;
                break;
            }
        }

        if (handToRemove.HasValue)
        {
            HAND hand = handToRemove.Value;

            grabbedObjects.Remove(hand);
            positionOffsets.Remove(hand);
            rotationOffsets.Remove(hand);
        }
    }
}