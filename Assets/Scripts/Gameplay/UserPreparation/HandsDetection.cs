using System;
using System.Collections;
using DG.Tweening;
using Leap;
using TMPro;
using UnityEngine;

public class HandsDetection : MonoBehaviour
{
    public event Action OnPreparationCompleted;

    [Header("Leap")]
    [SerializeField] private LeapServiceProvider provider;

    [Header("Left Hand")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Renderer left1;
    [SerializeField] private Renderer left2;

    [Header("Right Hand")]
    [SerializeField] private Transform rightHand;
    [SerializeField] private Renderer right1;
    [SerializeField] private Renderer right2;

    [Header("Colors")]
    [SerializeField] private Color detectedColor = Color.green;
    [SerializeField] private Color undetectedColor = Color.gray;
    [SerializeField] private float transitionDuration = 0.3f;


    [SerializeField] private TMP_Text messageText;

    private Coroutine countdownRoutine;

    private bool _leftDetected;
    private bool _rightDetected;
    private Material _leftMaterial;
    private Material _rightMaterial;

    private Tween _leftTween;
    private Tween _rightTween;

    private void Awake()
    {
        _leftMaterial = Instantiate(left1.sharedMaterial);
        _rightMaterial = Instantiate(right1.sharedMaterial);

        left1.material = _leftMaterial;
        left2.material = _leftMaterial;

        right1.material = _rightMaterial;
        right2.material = _rightMaterial;

        SetMaterialColor(_leftMaterial, undetectedColor);
        SetMaterialColor(_rightMaterial, undetectedColor);
    }

    private void Update()
    {
        bool leftDetected = false;
        bool rightDetected = false;

        Frame frame = provider.CurrentFrame;

        foreach (Hand hand in frame.Hands)
        {
            if (hand.IsLeft)
                leftDetected = true;
            else
                rightDetected = true;
        }

        if (leftDetected != _leftDetected)
        {
            bool wasDetected = _leftDetected;
            _leftDetected = leftDetected;

            AnimateHand(
                _leftMaterial,
                ref _leftTween,
                leftDetected ? detectedColor : undetectedColor
            );

            if (!wasDetected && leftDetected)
            {
                PunchHand(leftHand);
            }
        }

        if (rightDetected != _rightDetected)
        {
            bool wasDetected = _rightDetected;
            _rightDetected = rightDetected;

            AnimateHand(
                _rightMaterial,
                ref _rightTween,
                rightDetected ? detectedColor : undetectedColor
            );

            if (!wasDetected && rightDetected)
            {
                PunchHand(rightHand);
            }
        }
        CheckCountdown();

    }

    private void CheckCountdown()
    {
        bool bothHandsDetected = _leftDetected && _rightDetected;

        if (bothHandsDetected)
        {
            if (countdownRoutine == null)
                countdownRoutine = StartCoroutine(CountdownRoutine());
        }
        else
        {
            if (countdownRoutine != null)
            {
                StopCoroutine(countdownRoutine);
                countdownRoutine = null;
            }

            messageText.text = "Coloca ambas manos sobre el sensor";
        }
    }

    private IEnumerator CountdownRoutine()
    {
        for (int i = 3; i > 0; i--)
        {
            messageText.text = $"Preparando...\n{i}";
            yield return new WaitForSeconds(1f);

            if (!_leftDetected || !_rightDetected)
            {
                countdownRoutine = null;
                yield break;
            }
        }

        messageText.text = "¡Listo!";

        OnPreparationCompleted?.Invoke();

        countdownRoutine = null;
    }

    private void AnimateHand(
        Material material,
        ref Tween tween,
        Color targetColor)
    {
        tween?.Kill();

        Color currentColor = material.color;

        tween = DOTween.To(
                () => currentColor,
                c =>
                {
                    currentColor = c;
                    SetMaterialColor(material, c);
                },
                targetColor,
                transitionDuration)
            .SetEase(Ease.OutQuad);
    }

    private void PunchHand(Transform t)
    {
        t.DOKill();
        t.localScale = Vector3.one;
        t.DOPunchScale(
            Vector3.one * 0.08f,
            0.35f,
            8,
            0.8f);
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