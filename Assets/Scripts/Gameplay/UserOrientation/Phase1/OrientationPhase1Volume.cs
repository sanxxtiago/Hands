using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class OrientationPhase1Volume : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private OrientationPhase1HandTracker handTracker;
    [SerializeField] private HandType requiredHand = HandType.NONE;

    [Header("Touch")]
    [SerializeField, Min(0f)] private float touchDelay = 0.15f;

    [Header("Touch feedback")]
    [SerializeField] private ParticleSystem palmTouchEffectPrefab;

    public event Action<OrientationPhase1Volume> OnTouched;

    public HandType RequiredHand => requiredHand;
    public bool IsTouched => _touched;

    private BoxCollider _volumeCollider;
    private Coroutine _touchCoroutine;
    private bool _touchPending;
    private bool _touched;

    private void Awake()
    {
        _volumeCollider = GetComponent<BoxCollider>();

        if (handTracker == null)
        {
            Debug.LogError(
                "[OrientationPhase1Volume] Falta asignar el tracker de manos.",
                this);
        }
    }

    private void Update()
    {
        if (_touched || _touchPending || handTracker == null || _volumeCollider == null)
            return;

        if (!IsRequiredHandInside())
            return;

        _touchPending = true;
        _touchCoroutine = StartCoroutine(ConfirmTouchAfterDelay());
    }

    private IEnumerator ConfirmTouchAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, touchDelay));

        _touchCoroutine = null;
        _touchPending = false;

        if (_touched || !TryGetRequiredHandPositionInside(out Vector3 palmPosition))
            yield break;

        _touched = true;
        PlayPalmTouchEffect(palmPosition);
        OnTouched?.Invoke(this);
    }

    private bool IsRequiredHandInside()
    {
        return TryGetRequiredHandPositionInside(out _);
    }

    private bool TryGetRequiredHandPositionInside(out Vector3 palmPosition)
    {
        if (requiredHand == HandType.NONE)
        {
            if (TryGetHandPositionInside(HandType.LEFT, out palmPosition))
                return true;

            return TryGetHandPositionInside(HandType.RIGHT, out palmPosition);
        }

        return TryGetHandPositionInside(requiredHand, out palmPosition);
    }

    private bool TryGetHandPositionInside(HandType handType, out Vector3 palmPosition)
    {
        if (!handTracker.TryGetPalmPosition(handType, out palmPosition))
            return false;

        Vector3 localPosition = transform.InverseTransformPoint(palmPosition);
        Vector3 localCenter = _volumeCollider.center;
        Vector3 localHalfSize = _volumeCollider.size * 0.5f;

        return Mathf.Abs(localPosition.x - localCenter.x) <= localHalfSize.x &&
               Mathf.Abs(localPosition.y - localCenter.y) <= localHalfSize.y &&
               Mathf.Abs(localPosition.z - localCenter.z) <= localHalfSize.z;
    }

    private void PlayPalmTouchEffect(Vector3 palmPosition)
    {
        if (palmTouchEffectPrefab == null)
            return;

        ParticleSystem effect = Instantiate(
            palmTouchEffectPrefab,
            palmPosition,
            Quaternion.identity);
        effect.gameObject.SetActive(true);
        effect.Play(true);

        ParticleSystem.MainModule main = effect.main;
        float effectLifetime = main.duration +
                               main.startDelay.constantMax +
                               main.startLifetime.constantMax;
        Destroy(effect.gameObject, Mathf.Max(0.1f, effectLifetime));
    }

    private void OnEnable()
    {
        _touchPending = false;
        _touched = false;
    }

    private void OnDisable()
    {
        if (_touchCoroutine != null)
        {
            StopCoroutine(_touchCoroutine);
            _touchCoroutine = null;
        }

        _touchPending = false;
        _touched = false;
    }
}
