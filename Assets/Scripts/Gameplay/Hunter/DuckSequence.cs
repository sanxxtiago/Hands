using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct DuckSequenceStep
{
    public SpawnSide spawnSide;
    public HandType requiredHand;
    public float movementDuration;
    public float delayBeforeSpawn;
}

[CreateAssetMenu(fileName = "NewDuckSequence", menuName = "DuckHunter/Duck Sequence")]
public class DuckSequence : ScriptableObject
{
    public List<DuckSequenceStep> steps = new();
}