using System.Collections;
using System.IO;
using UnityEngine;

public class DotSpawner : MonoBehaviour
{
    public GameObject dotPrefab;
    public TargetDetector detector;
    public PathData[] paths;
    private bool isSpawning = false;
    private DotBehaviour dot = null;

    public void Spawn()
    {
        isSpawning = true;
        float randomX = Random.Range(BoundingBox.Instance.min.x, BoundingBox.Instance.max.x);
        float randomY = Random.Range(BoundingBox.Instance.min.y, BoundingBox.Instance.max.y);

        Vector3 randomPos = new Vector3(randomX, randomY, dotPrefab.transform.position.z);
        GameObject instance = Instantiate(dotPrefab, randomPos, Quaternion.identity);
        dot = instance.GetComponent<DotBehaviour>();
        PathData selectedPath = paths[Random.Range(0, paths.Length)];
        dot.Path = selectedPath;
        dot.IsTrackable = true;
        detector.target = dot;
    }
    void Update()
    {
        if (dot != null || !isSpawning) return;

        Spawn();
    }

}
//     IEnumerator SpawnDot()
//     {
//         while (true)
//         {

//             float randomX = Random.Range(BoundingBox.Instance.min.x, BoundingBox.Instance.max.x);
//             float randomY = Random.Range(BoundingBox.Instance.min.y, BoundingBox.Instance.max.y);

//             Vector3 randomPos = new Vector3(randomX, randomY, dotPrefab.transform.position.z);
//             GameObject instance = Instantiate(dotPrefab, randomPos, Quaternion.identity);
//             dot = instance.GetComponent<DotBehaviour>();
//             PathData selectedPath = paths[Random.Range(0, paths.Length)];
//             dot.Path = selectedPath;
//             dot.IsTrackable = true;
//             detector.target = dot;
//             //sumar tiempo que se demora en tocar

//             yield return null;
//         }
//     }


// }
