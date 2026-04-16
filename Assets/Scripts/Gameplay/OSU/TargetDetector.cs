using System.Collections.Generic;
using UnityEngine;

public class TargetDetector : MonoBehaviour
{
    public GameplayHandInput input;
    public List<DotBehaviour> dots = new List<DotBehaviour>();

    void Update()
    {
        foreach (DotBehaviour dot in dots)
        {
            if (!dot.IsHitted && input.LeftHandData != null)
            {
                Vector3 leftPos = input.LeftHandData.Value.position;

                if (Vector3.Distance(leftPos, dot.transform.position) <= dot.radius)
                {
                    dot.Hit();
                }
            }

            if (!dot.IsHitted && input.RightHandData != null)
            {
                Vector3 rightPos = input.RightHandData.Value.position;

                if (Vector3.Distance(rightPos, dot.transform.position) <= dot.radius)
                {
                    dot.Hit();
                }
            }
        }
    }

    public void AddDot(DotBehaviour dot)
    {
        dots.Add(dot);
    }
}