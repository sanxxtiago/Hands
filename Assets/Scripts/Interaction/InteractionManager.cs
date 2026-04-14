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
            grabbed.OnGrabStart(data);
            grabbed.OnForcedRelease += HandleForcedRelease;
        }

        if (e.phase == GesturePhase.UPDATE)
        {
            if (grabbed == null) return;

            grabbed.OnGrabUpdate(data);
        }

        if (e.phase == GesturePhase.END)
        {
            if (grabbed == null) return;

            grabbed.OnForcedRelease -= HandleForcedRelease;
            grabbed.OnGrabEnd(data);
            grabbed = null;
        }
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
            grabbed = null;
    }
}