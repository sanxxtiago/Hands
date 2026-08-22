using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OSUPhaseDefinition
{
    [SerializeField] private string phaseName = "Fase";
    [SerializeField] private List<OSUStep> steps = new();
    [SerializeField, Min(0f)] private float transitionDelay = 0.75f;

    public string PhaseName => phaseName;
    public IReadOnlyList<OSUStep> Steps => steps;
    public float TransitionDelay => transitionDelay;
    public int StepCount => steps?.Count ?? 0;
}

[CreateAssetMenu(
    fileName = "OSUSequence",
    menuName = "OSU/Sequence")]
public class OSUSequence : ScriptableObject
{
    [Tooltip("Profundidad Z comun para todos los puntos de la secuencia.")]
    [SerializeField] private float pointsDepth = 0.5f;
    [SerializeField] private List<OSUPhaseDefinition> phases = new();

    public float PointsDepth => pointsDepth;
    public IReadOnlyList<OSUPhaseDefinition> Phases => phases;
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
