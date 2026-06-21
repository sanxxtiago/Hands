using System.Collections.Generic;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public InteractionTracker leftTracker;
    public InteractionTracker rightTracker;

    public InteractionResolver resolver;

    public float grabRadius = 0.1f;
    public LayerMask interactableLayer;
    private Interactable grabbed;
    private Interactable activeTarget;

    void OnEnable()
    {
        leftTracker.OnInteraction += HandleInteraction;
        rightTracker.OnInteraction += HandleInteraction;
    }

    void OnDisable()
    {
        leftTracker.OnInteraction -= HandleInteraction;
        rightTracker.OnInteraction -= HandleInteraction;
    }

    void Awake()
    {
        leftTracker = new InteractionTracker(HandType.LEFT);
        rightTracker = new InteractionTracker(HandType.RIGHT);
        resolver = new InteractionResolver();
    }

    void OnDestroy()
    {
        leftTracker?.Dispose();
        rightTracker?.Dispose();
    }


    void HandleInteraction(InteractionEvent e)
    {
        //Debug.Log("Interaction received");
        var target = FindTarget(e);

        bool isGrabbing = grabbed != null;

        var resolved = resolver.Resolve(e, target, isGrabbing);

        ApplyInteraction(resolved);
    }

    Interactable FindTarget(InteractionEvent e)
    {
        Vector3 handPos = e.position;

        Collider[] hits = Physics.OverlapSphere(handPos, grabRadius, interactableLayer);

        float minDist = float.MaxValue;
        Interactable best = null;

        foreach (var hit in hits)
        {
            var interactable = hit.GetComponent<Interactable>();
            if (interactable == null) continue;

            float dist = Vector3.Distance(handPos, hit.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                best = interactable;
            }
        }
        //Debug.Log($"HITS: {hits.Length}");
        return best;
    }
    void ApplyInteraction(ResolvedInteraction r)
    {
        if (r.target == null) return;

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
        var data = new InteractableData(e.position, e.rotation);

        if (e.phase == GesturePhase.START)
        {
            if (grabbed != null) return;

            grabbed = r.target;
            activeTarget = grabbed;

            grabbed.OnGrabStart(data);
        }

        if (e.phase == GesturePhase.END)
        {
            var target = r.target;

            if (target != null)
            {
                target.OnForcedRelease -= HandleForcedRelease;
                target.OnGrabEnd(data);
            }

            grabbed = null;
        }

        if (e.phase == GesturePhase.END)
        {
            var target = activeTarget ?? r.target;

            if (target != null)
            {
                target.OnGrabEnd(data);
                target.OnForcedRelease -= HandleForcedRelease;
            }

            if (grabbed == target)
                grabbed = null;

            activeTarget = null;
        }
        //Debug.Log($"PHASE: {e.phase}, target: {r.target}");
    }

    void HandleRotate(ResolvedInteraction r)
    {
        var e = r.source;

        if (grabbed == null) return;
        if (e.phase != GesturePhase.UPDATE) return;

        var data = new InteractableData(e.position, e.rotation);

        grabbed.OnRotate(data);
    }

    void HandleSelect(ResolvedInteraction r)
    {
        var e = r.source;

        if (e.phase != GesturePhase.START) return;

        var data = new InteractableData(e.position, e.rotation);

        r.target.OnSelect(data);
    }

    void HandleForcedRelease(Interactable target)
    {
        if (grabbed == target)
        {
            activeTarget = target;
            grabbed = null;
        }
    }
}