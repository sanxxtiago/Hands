using DG.Tweening;
using UnityEngine;

public sealed class HandsPreparationVisuals : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private HandsDetection handsDetection;

    [Header("Left Hand")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Renderer left1;
    [SerializeField] private Renderer left2;

    [Header("Right Hand")]
    [SerializeField] private Transform rightHand;
    [SerializeField] private Renderer right1;
    [SerializeField] private Renderer right2;

    private Material leftMaterial;
    private Material rightMaterial;

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        leftMaterial = Instantiate(left1.sharedMaterial);
        rightMaterial = Instantiate(right1.sharedMaterial);

        left1.material = leftMaterial;
        left2.material = leftMaterial;
        right1.material = rightMaterial;
        right2.material = rightMaterial;

        SetMaterialColor(leftMaterial, HandsColor.Left);
        SetMaterialColor(rightMaterial, HandsColor.Right);
    }

    private void OnEnable()
    {
        if (handsDetection == null)
            return;

        handsDetection.OnLeftHandDetectionChanged += HandleLeftHandDetectionChanged;
        handsDetection.OnRightHandDetectionChanged += HandleRightHandDetectionChanged;
    }

    private void OnDisable()
    {
        if (handsDetection != null)
        {
            handsDetection.OnLeftHandDetectionChanged -= HandleLeftHandDetectionChanged;
            handsDetection.OnRightHandDetectionChanged -= HandleRightHandDetectionChanged;
        }

        leftHand?.DOKill();
        rightHand?.DOKill();
    }

    private void OnDestroy()
    {
        if (leftMaterial != null)
            Destroy(leftMaterial);

        if (rightMaterial != null)
            Destroy(rightMaterial);
    }

    private void HandleLeftHandDetectionChanged(bool detected)
    {
        if (detected)
            PunchHand(leftHand);
    }

    private void HandleRightHandDetectionChanged(bool detected)
    {
        if (detected)
            PunchHand(rightHand);
    }

    private static void PunchHand(Transform hand)
    {
        if (hand == null)
            return;

        hand.DOKill();
        hand.localScale = Vector3.one;
        hand.DOPunchScale(
            Vector3.one * 0.08f,
            0.35f,
            8,
            0.8f);
    }

    private bool ValidateReferences()
    {
        if (handsDetection == null)
        {
            Debug.LogError(
                "[HandsPreparationVisuals] Falta asignar HandsDetection.",
                this);
            return false;
        }

        if (leftHand == null || left1 == null || left2 == null ||
            rightHand == null || right1 == null || right2 == null)
        {
            Debug.LogError(
                "[HandsPreparationVisuals] Faltan referencias de los modelos de las manos.",
                this);
            return false;
        }

        if (left1.sharedMaterial == null || right1.sharedMaterial == null)
        {
            Debug.LogError(
                "[HandsPreparationVisuals] Falta el material compartido de una mano.",
                this);
            return false;
        }

        return true;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        material.color = color;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }
}
