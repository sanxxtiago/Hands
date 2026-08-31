using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public enum OrientationPhase3State
{
    ReadyToGrab,
    Moving,
    ReadyToRelease,
    Completed
}

public class OrientationPhase3Manager : OrientationManager
{
    private const string MsgTakePiece = "Usa una de tus manos para tomar la pieza";
    private const string MsgMoveToTarget = "Mueve la pieza hacia el objetivo";
    private const string MsgReleasePiece = "Suelta la pieza";
    private const string MsgPhaseCompleted = "¡Has completado la fase!";

    [SerializeField] private GameObject piece;
    [SerializeField] private GameObject target;
    [SerializeField] private Vector3 minSpawnPos;
    [SerializeField] private Vector3 maxSpawnPos;
    [SerializeField] private float minDistance = 0.3f;
    [SerializeField] private int maxAttempts = 50;
    [SerializeField] private TMP_Text message;
    [SerializeField] private Transition transition;
    [SerializeField, Min(0f)] private float completedMessageDuration = 1f;
    [SerializeField, Min(0f)] private float modalDelay = 2f;

    public event Action<OrientationPhase3State> OnStateChanged;
    public event Action<OrientationPieceBehaviour, OrientationSlotBehaviour> OnObjectsSpawned;

    private OrientationSlotBehaviour targetBehaviour;
    private OrientationPieceBehaviour spawnedPiece;
    private OrientationPhase3State currentState;
    private bool hasPublishedState;
    private bool isCompletionStarted;

    public OrientationPhase3State CurrentState => currentState;
    public bool IsCompletionStarted => isCompletionStarted;

    private void Start()
    {
        SpawnObjects();
        SetState(OrientationPhase3State.ReadyToGrab);
        transition?.FadeOut();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        UnsubscribeFromPiece();
        UnsubscribeFromSlot();
    }

    private void SpawnObjects()
    {
        Vector3 piecePos = GetRandomPosition(piece.transform.position.y);

        Vector3 targetPos = Vector3.zero;
        Vector3 bestCandidate = Vector3.zero;
        float bestDistance = -1f;
        bool foundValidPosition = false;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 candidate = GetRandomPosition(target.transform.position.y);
            float distance = Vector3.Distance(piecePos, candidate);

            if (distance > bestDistance)
            {
                bestDistance = distance;
                bestCandidate = candidate;
            }

            if (distance >= minDistance)
            {
                targetPos = candidate;
                foundValidPosition = true;
                break;
            }
        }

        if (!foundValidPosition)
        {
            targetPos = bestCandidate;
            Debug.LogWarning(
                $"[OrientationPhase3] No se encontró una posición con distancia mínima de {minDistance}. " +
                $"Usando la mejor encontrada ({bestDistance:F2})."
            );
        }

        GameObject pieceInstance = Instantiate(piece, piecePos, Quaternion.identity);
        spawnedPiece = pieceInstance.GetComponent<OrientationPieceBehaviour>();
        if (spawnedPiece != null)
        {
            spawnedPiece.OnGrabbed += HandlePieceGrabbed;
            spawnedPiece.OnReleased += HandlePieceReleased;
        }

        GameObject targetInstance = Instantiate(target, targetPos, Quaternion.identity);
        targetBehaviour = targetInstance.GetComponent<OrientationSlotBehaviour>();
        if (targetBehaviour != null)
        {
            targetBehaviour.OnPieceEntered += HandlePieceEntered;
            targetBehaviour.OnPieceExited += HandlePieceExited;
            targetBehaviour.OnPieceFitted += HandlePieceFitted;
        }

        if (spawnedPiece != null && targetBehaviour != null)
            OnObjectsSpawned?.Invoke(spawnedPiece, targetBehaviour);
    }

    private void UnsubscribeFromPiece()
    {
        if (spawnedPiece == null) return;
        spawnedPiece.OnGrabbed -= HandlePieceGrabbed;
        spawnedPiece.OnReleased -= HandlePieceReleased;
    }

    private void UnsubscribeFromSlot()
    {
        if (targetBehaviour == null) return;
        targetBehaviour.OnPieceEntered -= HandlePieceEntered;
        targetBehaviour.OnPieceExited -= HandlePieceExited;
        targetBehaviour.OnPieceFitted -= HandlePieceFitted;
    }

    private Vector3 GetRandomPosition(float yPos)
    {
        return new Vector3(
            Random.Range(minSpawnPos.x, maxSpawnPos.x),
            yPos,
            Random.Range(minSpawnPos.z, maxSpawnPos.z)
        );
    }

    private void HandlePieceGrabbed()
    {
        if (isCompletionStarted) return;
        SetState(OrientationPhase3State.Moving);
    }

    private void HandlePieceReleased()
    {
        if (isCompletionStarted) return;
        SetState(OrientationPhase3State.ReadyToGrab);
    }

    private void HandlePieceEntered()
    {
        if (isCompletionStarted) return;
        SetState(OrientationPhase3State.ReadyToRelease);
    }

    private void HandlePieceExited()
    {
        if (isCompletionStarted) return;
        SetState(spawnedPiece != null && spawnedPiece.IsGrabbed
            ? OrientationPhase3State.Moving
            : OrientationPhase3State.ReadyToGrab);
    }

    private void HandlePieceFitted()
    {
        if (isCompletionStarted) return;
        isCompletionStarted = true;
        SetState(OrientationPhase3State.Completed);
        StartCoroutine(CompletePhaseSequence());
    }

    private IEnumerator CompletePhaseSequence()
    {
        MarkPhaseCompleted();
        yield return new WaitForSeconds(completedMessageDuration);
        yield return new WaitForSeconds(Mathf.Max(0f, modalDelay - completedMessageDuration));
        CompletePhase();
    }

    private void MarkPhaseCompleted()
    {
        UnsubscribeFromPiece();
        UnsubscribeFromSlot();
    }

    protected override void CompletePhase()
    {
        base.CompletePhase();
    }

    private void SetState(OrientationPhase3State state)
    {
        if (hasPublishedState && currentState == state)
            return;

        currentState = state;
        hasPublishedState = true;
        SetMessage(GetMessage(state));
        OnStateChanged?.Invoke(state);
    }

    private static string GetMessage(OrientationPhase3State state)
    {
        switch (state)
        {
            case OrientationPhase3State.Moving:
                return MsgMoveToTarget;
            case OrientationPhase3State.ReadyToRelease:
                return MsgReleasePiece;
            case OrientationPhase3State.Completed:
                return MsgPhaseCompleted;
            default:
                return MsgTakePiece;
        }
    }

    private void SetMessage(string text)
    {
        if (message == null) return;
        message.text = text;
    }
}
