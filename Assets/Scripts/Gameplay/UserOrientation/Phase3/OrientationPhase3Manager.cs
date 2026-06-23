using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class OrientationPhase3Manager : OrientationManager
{
    [SerializeField] private GameObject piece;
    [SerializeField] private GameObject target;
    [SerializeField] private Vector3 minSpawnPos;
    [SerializeField] private Vector3 maxSpawnPos;
    [SerializeField] private float minDistance = 0.3f;
    [SerializeField] private int maxAttempts = 50;
    [SerializeField] private TMP_Text message;
    [SerializeField] private Transition transition;

    private OrientationSlotBehaviour targetBehaviour;

    public event Action OnOrientationFinished;
    void Start()
    {
        SpawnObjects();
        transition.FadeOut();
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
                $"No se encontró una posición con distancia mínima de {minDistance}. " +
                $"Usando la mejor encontrada ({bestDistance:F2})."
            );
        }

        Instantiate(piece, piecePos, Quaternion.identity);
        GameObject targetInstance = Instantiate(target, targetPos, Quaternion.identity);
        targetBehaviour = targetInstance.GetComponent<OrientationSlotBehaviour>();
        if (targetBehaviour != null)
        {
            targetBehaviour.OnPieceEntered += UpdateMessage;
            targetBehaviour.OnPieceFitted += CompletePhase;
            targetBehaviour.OnPieceExited += UpdateMessage1;
        }
    }

    private Vector3 GetRandomPosition(float yPos)
    {
        return new Vector3(
            Random.Range(minSpawnPos.x, maxSpawnPos.x),
            yPos,
            Random.Range(minSpawnPos.z, maxSpawnPos.z)
        );
    }

    protected override void CompletePhase()
    {
        StartCoroutine(OrientationCompleted());
    }

    IEnumerator OrientationCompleted()
    {
        message.text = "¡Has completado las fases de familiarización!";
        yield return new WaitForSeconds(3f);
        base.CompletePhase();

    }

    private void UpdateMessage()
    {
        message.text = "Suelta la pieza dentro de la zona...";
    }

    private void UpdateMessage1()
    {
        message.text = "Mueve la pieza hacia la zona indicada...";
    }
}
