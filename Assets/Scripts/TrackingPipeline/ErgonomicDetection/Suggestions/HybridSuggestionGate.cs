using System;

public sealed class HybridSuggestionGate
{
    private readonly float cooldownSeconds;
    private readonly int maximum;
    private readonly bool[,] emitted = new bool[2, 4];
    private float lastEmission;
    public int EmittedCount { get; private set; }

    public HybridSuggestionGate(HybridSuggestionProfile profile)
    {
        if (profile == null || !profile.TryValidate(out _))
            throw new ArgumentException("Perfil híbrido inválido.");
        cooldownSeconds = profile.CooldownSeconds;
        maximum = profile.MaximumSuggestionsPerExercise;
        Reset();
    }

    public void Reset()
    {
        Array.Clear(emitted, 0, emitted.Length);
        lastEmission = float.NegativeInfinity;
        EmittedCount = 0;
    }

    public bool TrySelect(HybridSuggestionData? left, HybridSuggestionData? right,
        float now, out HybridSuggestionData selected)
    {
        selected = default;
        if (float.IsNaN(now) || float.IsInfinity(now) ||
            EmittedCount >= maximum || now - lastEmission < cooldownSeconds)
            return false;

        bool hasLeft = Eligible(left);
        bool hasRight = Eligible(right);
        bool protectionActive = (left.HasValue && left.Value.priority > 0) ||
            (right.HasValue && right.Value.priority > 0);
        if (protectionActive)
        {
            hasLeft &= left.HasValue && left.Value.priority > 0;
            hasRight &= right.HasValue && right.Value.priority > 0;
        }
        if (!hasLeft && !hasRight) return false;

        selected = hasLeft ? left.Value : right.Value;
        if (hasLeft && hasRight && (right.Value.priority > selected.priority ||
            (right.Value.priority == selected.priority && right.Value.conditionSeconds > selected.conditionSeconds)))
            selected = right.Value;

        emitted[selected.HandType == HandType.LEFT ? 0 : 1, (int)selected.type] = true;
        lastEmission = now;
        EmittedCount++;
        return true;
    }

    private bool Eligible(HybridSuggestionData? candidate)
    {
        if (!candidate.HasValue) return false;
        HybridSuggestionData value = candidate.Value;
        int hand = value.HandType == HandType.LEFT ? 0 : (value.HandType == HandType.RIGHT ? 1 : -1);
        int type = (int)value.type;
        return hand >= 0 && type >= 0 && type < 4 && !emitted[hand, type];
    }
}
