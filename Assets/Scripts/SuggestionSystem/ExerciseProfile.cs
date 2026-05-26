using UnityEngine;

[CreateAssetMenu(fileName = "ExerciseProfile", menuName = "Profiles")]
public class ExerciseProfile : ScriptableObject
{
    public HandProfile leftHand;
    public HandProfile rightHand;

    private void OnValidate()
    {
        leftHand?.NormalizeZones();
        rightHand?.NormalizeZones();
    }

}
[System.Serializable]
public class HandProfile
{
    public bool isActive; //Para ejercicios unilaterales
    public ZoneTarget hand;
    public ZoneTarget wrist;
    public ZoneTarget forearm;
    [Range(0f, 1f)]
    public float minActivity = 0.4f;
    public void NormalizeZones()
    {
        float total = hand.editorValue + wrist.editorValue + forearm.editorValue;

        if (total <= 0f)
        {
            // fallback seguro
            hand.normalized = 0.33f;
            wrist.normalized = 0.33f;
            forearm.normalized = 0.34f;
            return;
        }

        hand.normalized = hand.editorValue / total;
        wrist.normalized = wrist.editorValue / total;
        forearm.normalized = forearm.editorValue / total;
        ClampTolerances();
    }

    private void ClampTolerances()
    {
        hand.tolerance = Mathf.Clamp01(hand.tolerance);
        wrist.tolerance = Mathf.Clamp01(wrist.tolerance);
        forearm.tolerance = Mathf.Clamp01(forearm.tolerance);
    }
}

[System.Serializable]
public class ZoneTarget
{
    [Range(0, 100)]
    public float editorValue;   // Input humano

    [HideInInspector]
    public float normalized;    // Uso interno

    [Range(0f, 1f)]
    public float tolerance;     // coherente con escala normalizada
}