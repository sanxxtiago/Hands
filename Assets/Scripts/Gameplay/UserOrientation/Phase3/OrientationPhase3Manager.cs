using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

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

    private OrientationSlotBehaviour targetBehaviour;
    private OrientationPieceBehaviour spawnedPiece;

    void Start()
    {
        SpawnObjects();
        SetMessage(MsgTakePiece);
        transition.FadeOut();
    }

    private void OnDestroy()
    {
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
        if (IsPhaseCompleted()) return;
        SetMessage(MsgMoveToTarget);
    }

    private void HandlePieceReleased()
    {
        if (IsPhaseCompleted()) return;
        SetMessage(MsgTakePiece);
    }

    private void HandlePieceEntered()
    {
        if (IsPhaseCompleted()) return;
        SetMessage(MsgReleasePiece);
    }

    private void HandlePieceExited()
    {
        if (IsPhaseCompleted()) return;
        SetMessage(spawnedPiece != null && spawnedPiece.IsGrabbed ? MsgMoveToTarget : MsgTakePiece);
    }

    private void HandlePieceFitted()
    {
        if (IsPhaseCompleted()) return;
        StartCoroutine(CompletePhaseSequence());
    }

    private IEnumerator CompletePhaseSequence()
    {
        MarkPhaseCompleted();
        SetMessage(MsgPhaseCompleted);
        yield return new WaitForSeconds(completedMessageDuration);
        yield return new WaitForSeconds(Mathf.Max(0f, modalDelay - completedMessageDuration));
        CompletePhase();
    }

    private bool IsPhaseCompleted() => message != null && message.text == MsgPhaseCompleted;

    private void MarkPhaseCompleted()
    {
        UnsubscribeFromPiece();
        UnsubscribeFromSlot();
    }

    protected override void CompletePhase()
    {
        base.CompletePhase();
    }

    private void SetMessage(string text)
    {
        if (message == null) return;
        message.text = text;
    }
}
