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

[Serializable]
public class DuckPhaseDefinition
{
    [SerializeField] private string phaseName = "Fase";
    [SerializeField] private List<DuckSequenceStep> steps = new();
    [SerializeField, Min(0f)] private float transitionDelay = 0.75f;

    public string PhaseName => phaseName;
    public IReadOnlyList<DuckSequenceStep> Steps => steps;
    public float TransitionDelay => transitionDelay;
    public int StepCount => steps?.Count ?? 0;
}

[CreateAssetMenu(fileName = "NewDuckSequence", menuName = "DuckHunter/Duck Sequence")]
public class DuckSequence : ScriptableObject
{
    [SerializeField] private List<DuckPhaseDefinition> phases = new();

    public IReadOnlyList<DuckPhaseDefinition> Phases => phases;
    public int PhaseCount => phases?.Count ?? 0;

    public int[] GetPhaseTargets()
    {
        if (phases == null)
            return Array.Empty<int>();

        int[] targets = new int[phases.Count];

        for (int i = 0; i < phases.Count; i++)
            targets[i] = phases[i]?.StepCount ?? 0;

        return targets;
    }
}
