using System;
using UnityEngine;

public class DuckBehaviour : MonoBehaviour
{
    public event Action<DuckBehaviour> OnReachedDestination;
    public event Action<DuckBehaviour> OnHit;

    // Ya no las exponemos en el inspector, el pato las calcula internamente
    private Vector3 startPoint;
    private Vector3 endPoint;
    private int floor;
    private float duration;
    private HandType requiredHand;
    private float elapsedTime;
    private bool isMoving;
    private bool isHit = false;
    [SerializeField] private Renderer body;
    [SerializeField] private Renderer wings;

    void Awake()
    {
        if (body == null)
            body = GetComponent<Renderer>();
    }

    public void Initialize(SpawnSide side, HandType requiredHand, float duration, Vector3 leftWorldBound, Vector3 rightWorldBound)
    {
        this.duration = Mathf.Max(0.01f, duration);
        this.requiredHand = requiredHand;
        //this.floor = floor;
        // El pato abstrae su origen y destino basado en el enum
        if (side == SpawnSide.Left)
        {
            startPoint = leftWorldBound;
            endPoint = rightWorldBound;
            transform.rotation = Quaternion.Euler(0, 90, 0);
        }
        else // SpawnSide.Right
        {
            startPoint = rightWorldBound;
            endPoint = leftWorldBound;
            transform.rotation = Quaternion.Euler(0, -90, 0);

        }

        transform.position = startPoint;

        elapsedTime = 0f;
        isMoving = true;
        isHit = false;
        switch (this.requiredHand)
        {
            case HandType.NONE:
                SetPieceColor(HandsColor.Default);
                break;
            case HandType.LEFT:
                SetPieceColor(HandsColor.Left);
                break;
            case HandType.RIGHT:
                SetPieceColor(HandsColor.Right);
                break;
        }
    }

    private void Update()
    {
        if (!isMoving)
            return;

        elapsedTime += Time.deltaTime;

        float t = Mathf.Clamp01(elapsedTime / duration);

        transform.position = Vector3.Lerp(startPoint, endPoint, t);

        if (t >= 1f)
        {
            isMoving = false;
            OnReachedDestination?.Invoke(this);
        }
    }

    public void Hit(HandType requiredHand)
    {
        if (this.requiredHand != requiredHand && this.requiredHand != HandType.NONE)
            return;

        if (isHit)
            return;

        isHit = true;
        isMoving = false;
        Debug.Log("¡PATO CAZADO!");

        OnHit?.Invoke(this);
    }

    private void SetPieceColor(Color color)
    {
        body.material.color = color;
        wings.material.color = color;

    }
}