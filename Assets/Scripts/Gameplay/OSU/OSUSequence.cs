using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "OSUSequence",
    menuName = "OSU/Sequence")]
public class OSUSequence : ScriptableObject
{
    public List<OSUStep> steps = new();
}