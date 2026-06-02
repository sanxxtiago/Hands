using UnityEditor;
using UnityEngine;

public class TimeScaleUtil : MonoBehaviour
{
    public float TargetTime = 10f;
    public float timeElapsed = 0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timeElapsed += Time.deltaTime;
        if (timeElapsed > TargetTime){
            EditorApplication.isPaused = true;
            Debug.Log("YA pasaron");
        }
    }
}
