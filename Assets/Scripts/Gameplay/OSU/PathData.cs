using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "OSU/Path")]
public class PathData : ScriptableObject
{
    [SerializeField]
    public List<BezierCurveData> curves;
    public float speed = 0.25f;
}

