using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class ResultsUI : MonoBehaviour
{
    private ErgonomicsTracker LeftTracker => TrackerManager.Instance.left;
    private ErgonomicsTracker RightTracker => TrackerManager.Instance.right;
    public GameManager gameManager;
    public ArmResultUI rightArmAbsoluteResult;
    public ArmResultUI rightArmRelativeResult;

    public ArmResultUI leftArmAbsoluteResult;
    public ArmResultUI leftArmRelativeResult;


    public CanvasGroup group;

    void OnEnable()
    {
        gameManager.OnResults += ShowResults;
    }

    void OnDisable()
    {
        gameManager.OnResults -= ShowResults;
    }
    void Start()
    {
        group.alpha = 0f;
    }

    void ShowResults()
    {
        var rightAbsoluteData = RightTracker.GetAbsoluteUsage();
        var rightRelativeData = RightTracker.GetRelativeDistribution();

        var leftAbsoluteData = LeftTracker.GetAbsoluteUsage();
        var leftRelativeData = LeftTracker.GetRelativeDistribution();

        float rightActivity = RightTracker.GetActivePercentage();
        float rightInactivity = RightTracker.GetInactivePercentage();

        float leftActivity = LeftTracker.GetActivePercentage();
        float leftInactivity = LeftTracker.GetInactivePercentage();

        group.DOKill();
        group.alpha = 0;
        group.DOFade(1, 0.3f);

        //setea la información absoluta
        rightArmAbsoluteResult.PaintOnResults(rightAbsoluteData);
        rightArmAbsoluteResult.SetTextResult(rightAbsoluteData, rightActivity, rightInactivity, true);
        leftArmAbsoluteResult.PaintOnResults(leftAbsoluteData);
        leftArmAbsoluteResult.SetTextResult(leftAbsoluteData, leftActivity, leftInactivity, true);

        //setea la información relativa
        rightArmRelativeResult.PaintOnResults(rightRelativeData);
        rightArmRelativeResult.SetTextResult(rightRelativeData, 0, 0, false);
        leftArmRelativeResult.PaintOnResults(leftRelativeData);
        leftArmRelativeResult.SetTextResult(leftRelativeData, 0, 0, false);
    }

}
