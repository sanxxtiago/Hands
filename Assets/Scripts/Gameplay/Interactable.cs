using System.Collections;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public event System.Action<Interactable> OnForcedRelease;

    private Collider[] clampColliders;

    [Header("Ajuste de agarre")]
    [Tooltip("Desplazamiento local desde la palma hasta el punto donde la pieza queda bien agarrada.")]
    [SerializeField] private Vector3 grabAnchorOffset = new(0f, -0.05f, 0.04f);

    [Tooltip("Radio libre alrededor del punto de agarre: si la pieza ya esta dentro no se corrige nada.")]
    [SerializeField, Min(0f)] private float grabFreeRadius = 0.015f;

    [Tooltip("Proporcion del exceso fuera del radio libre que se corrige. Nunca centra del todo.")]
    [SerializeField, Range(0f, 1f)] private float grabPullStrength = 0.7f;

    [Tooltip("Desplazamiento maximo de la correccion en metros.")]
    [SerializeField, Min(0f)] private float grabMaxPull = 0.04f;

    [Tooltip("Duracion de la correccion suave al agarrar. La rotacion no se toca.")]
    [SerializeField, Min(0f)] private float grabSettleDuration = 0.12f;

    private Coroutine grabAdjustRoutine;

    // =========================
    // CAPABILITIES
    // =========================
    public virtual bool CanInteract(InteractionType interactionType) => true;
    public virtual bool CanInteract(InteractionType interactionType, HandType handType) =>
        CanInteract(interactionType);

    // =========================
    // GRAB
    // =========================
    public virtual void OnGrabStart(InteractableData handData)
    {
        PlayGrabAnchorAdjustment(handData);
    }

    public virtual void OnGrabEnd()
    {
        CancelGrabAnchorAdjustment();
    }

    // =========================
    // ROTATE
    // =========================
    public virtual void OnRotate(InteractableData data){ }

    // =========================
    // PINCH / SELECT
    // =========================
    public virtual void OnSelect(InteractableData data) { }

    // =========================
    // FORCE RELEASE
    // =========================
    public virtual void ForceRelease()
    {
        OnForcedRelease?.Invoke(this);
    }

    // =========================
    // UTILS
    // =========================
    protected Vector3 ClampPosition(Vector3 worldPosition)
    {
        if (BoundingBox.Instance == null)
            return worldPosition;

        clampColliders ??= GetComponentsInChildren<Collider>();

        return BoundingBox.Instance.ClampInsideBox(
            worldPosition,
            transform,
            clampColliders);
    }

    // =========================
    // AJUSTE SUAVE DE AGARRE
    // =========================
    // Acerca un poco la pieza al interior de la palma al agarrar para que no
    // quede pillada solo por un lado. Es una correccion parcial (zona libre +
    // tope maximo) y nunca toca la rotacion.
    protected void CancelGrabAnchorAdjustment()
    {
        if (grabAdjustRoutine == null)
            return;

        StopCoroutine(grabAdjustRoutine);
        grabAdjustRoutine = null;
    }

    private void PlayGrabAnchorAdjustment(InteractableData handData)
    {
        CancelGrabAnchorAdjustment();

        Vector3 anchor = handData.position + handData.rotation * grabAnchorOffset;
        Vector3 toPiece = transform.position - anchor;
        float distance = toPiece.magnitude;

        if (distance <= grabFreeRadius || distance < 1e-5f)
            return;

        float excess = distance - grabFreeRadius;
        float pull = Mathf.Min(excess * grabPullStrength, grabMaxPull);

        if (pull <= 0.0005f)
            return;

        Vector3 target = transform.position - toPiece / distance * pull;

        if (grabSettleDuration <= 0f)
        {
            transform.position = target;
            return;
        }

        grabAdjustRoutine = StartCoroutine(GrabAnchorSettle(target, grabSettleDuration));
    }

    private IEnumerator GrabAnchorSettle(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float smoothT = t >= 1f ? 1f : t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(start, target, smoothT);
            yield return null;
        }

        transform.position = target;
        grabAdjustRoutine = null;
    }
}
