using UnityEngine;

// Retícula en espacio de mundo que se ancla al punto final del láser.
// Recibe su pose por push desde HandLaserPointer; no hace físicas propias.
[RequireComponent(typeof(SpriteRenderer))]
public sealed class HandLaserReticle : MonoBehaviour
{
    [Tooltip("Tamaño aparente (en metros a un metro de distancia) usado para compensar la perspectiva.")]
    [SerializeField, Min(0.001f)] private float apparentSize = 0.05f;
    [Tooltip("Distancia mínima usada al compensar el tamaño; evita retículas gigantes al pegar muy cerca.")]
    [SerializeField, Min(0.01f)] private float minCompensationDistance = 0.3f;
    [Tooltip("Separación sobre la superficie para evitar z-fighting con paredes y patos.")]
    [SerializeField, Min(0f)] private float surfaceOffset = 0.01f;
    [Tooltip("Sprite blanco del anillo; su color se ajusta desde el SpriteRenderer.")]
    [SerializeField] private Sprite reticleSprite;

    private SpriteRenderer spriteRenderer;
    private Transform cachedTransform;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (reticleSprite != null)
            spriteRenderer.sprite = reticleSprite;

        cachedTransform = transform;
        Hide();
    }

    public void Show()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
    }

    public void Hide()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    // point: punto de impacto o fin del rayo. facing: normal de la superficie si
    // alignToSurface, o dirección del rayo en caso contrario. distance: longitud recorrida.
    public void UpdatePose(Vector3 point, Vector3 facing, float distance, bool alignToSurface)
    {
        if (spriteRenderer == null || !spriteRenderer.enabled)
            return;

        float compensationDistance = Mathf.Max(minCompensationDistance, distance);
        float scale = apparentSize * compensationDistance;
        cachedTransform.localScale = new Vector3(scale, scale, 1f);

        if (alignToSurface)
        {
            cachedTransform.position = point + facing * surfaceOffset;
            cachedTransform.rotation = Quaternion.LookRotation(-facing);
        }
        else
        {
            // Sin superficie: flota al final del láser mirando hacia atrás.
            cachedTransform.position = point;
            cachedTransform.rotation = Quaternion.LookRotation(-facing);
        }
    }
}
