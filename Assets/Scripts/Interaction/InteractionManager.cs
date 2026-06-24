using System.Collections.Generic;
using Leap;
using Leap.PhysicalHands;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public InteractionTracker tracker;
    //public InteractionTracker rightTracker;

    public InteractionResolver resolver;
    public HandType handType;

    [SerializeField] private float grabRadius = 0.1f;
    [SerializeField] private Vector3 grabOffset = new(0f, 0f, 0.05f);
    [SerializeField] private LayerMask interactableLayer;

    private Interactable grabbed;

    // Debug
    [SerializeField] private bool debug = true;
    [SerializeField] private Transform sphereDebug;

    private Vector3 debugHandPos;
    //private Vector3 rightDebugPos;

    private bool hasDebug;
    //private bool hasRightDebug;

    void OnEnable()
    {
        tracker.OnInteraction += HandleInteraction;
        //rightTracker.OnInteraction += HandleInteraction;
    }

    void OnDisable()
    {
        tracker.OnInteraction -= HandleInteraction;
        //7rightTracker.OnInteraction -= HandleInteraction;
    }

    void Awake()
    {
        tracker = new InteractionTracker(handType);
        //rightTracker = new InteractionTracker(HandType.RIGHT);

        resolver = new InteractionResolver();
    }

    void OnDestroy()
    {
        tracker?.Dispose();
        //rightTracker?.Dispose();
    }

    void Update()
    {
        sphereDebug.gameObject.SetActive(debug);

        //CheckGrabConsistency();
    }

    void HandleInteraction(InteractionEvent e)
    {

        //         Debug.Log(
        //      $"EVENT -> Hand:{e.handType} " +
        //      $"Type:{e.type} " +
        //      $"Phase:{e.phase} " +
        //      $"Palm:{e.palmPosition}"
        //  );

        var target = FindTarget(e);

        //Debug.Log($"TARGET FOUND: {(target ? target.name : "NULL")}");

        bool isGrabbing = grabbed != null;

        var resolved = resolver.Resolve(e, target, isGrabbing);

        ApplyInteraction(resolved);
    }

    Interactable FindTarget(InteractionEvent e)
    {
        Vector3 sphereCenter =
            e.palmPosition +
            e.palmRotation * grabOffset;

        debugHandPos = sphereCenter;
        hasDebug = true;

        if (sphereDebug != null)
            sphereDebug.position = sphereCenter;

        Collider[] hits = Physics.OverlapSphere(
            sphereCenter,
            grabRadius,
            interactableLayer
        );

        float minDist = float.MaxValue;
        Interactable best = null;

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Interactable>(out var interactable))
                continue;

            float dist = Vector3.Distance(
                sphereCenter,
                hit.ClosestPoint(sphereCenter)
            );

            if (dist < minDist)
            {
                minDist = dist;
                best = interactable;
            }
        }

        return best;
    }

    void CheckGrabConsistency()
    {
        if (grabbed == null)
            return;

        Collider[] hits = Physics.OverlapSphere(
            debugHandPos,
            grabRadius,
            interactableLayer
        );

        bool stillInside = false;

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Interactable>(out var interactable))
                continue;

            if (interactable == grabbed)
            {
                stillInside = true;
                return;
            }
        }

        if (!stillInside)
        {
            //Debug.Log($"Force Release: {grabbed.name}");

            grabbed.ForceRelease();
        }
    }

    void ApplyInteraction(ResolvedInteraction r)
    {
        //($"HAND: {r.source.handType}");

        if (r.target == null)
        {
            //Debug.Log($"TARGET: NULL");

            return;

        }
        //Debug.Log($"TARGET: {r.target.name}");
        switch (r.type)
        {
            case InteractionType.Grab:
                HandleGrab(r);
                break;

            case InteractionType.Rotate:
                HandleRotate(r);
                break;

            case InteractionType.Select:
                HandleSelect(r);
                break;
        }
    }

    void HandleGrab(ResolvedInteraction r)
    {
        var e = r.source;

        if (e.phase == GesturePhase.START)
        {
            if (grabbed != null)
                return;

            grabbed = r.target;
            grabbed.OnForcedRelease += HandleForcedRelease;

            //Debug.Log("Grabbed: " + grabbed.name);
            grabbed.OnGrabStart();
        }

        if (e.phase == GesturePhase.END)
        {
            if (grabbed != null)
            {
                grabbed.OnForcedRelease -= HandleForcedRelease;
                grabbed.OnGrabEnd();
            }
            grabbed = null;
        }
    }

    void HandleRotate(ResolvedInteraction r)
    {
        var e = r.source;

        if (grabbed == null) return;
        if (e.phase != GesturePhase.UPDATE) return;

        var data = new InteractableData(
            e.palmPosition,
            e.palmRotation
        );

        grabbed.OnRotate(data);
    }

    void HandleSelect(ResolvedInteraction r)
    {
        var e = r.source;

        if (e.phase != GesturePhase.START)
            return;

        var data = new InteractableData(
            e.palmPosition,
            e.palmRotation
        );

        r.target.OnSelect(data);
    }

    void HandleForcedRelease(Interactable target)
    {
        if (grabbed != target)
            return;

        grabbed.OnGrabEnd();
        grabbed.OnForcedRelease -= HandleForcedRelease;
        grabbed = null;
    }

    void OnDrawGizmos()
    {
        if (!hasDebug)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(debugHandPos, grabRadius);
    }
}