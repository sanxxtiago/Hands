using UnityEngine;

public class TrackerManager : MonoBehaviour
{
    public static TrackerManager Instance;

    public ErgonomicsTracker left;
    public ErgonomicsTracker right;

    void Awake()
    {
        Instance = this;
    }
}