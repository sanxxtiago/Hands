using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Path")]
public class PathData : ScriptableObject
{
    [SerializeField]
    public List<BezierCurveData> curves;
    public float duration;

}