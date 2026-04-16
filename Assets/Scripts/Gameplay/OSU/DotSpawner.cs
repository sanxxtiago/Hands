using System.Collections;
using System.IO;
using UnityEngine;

public class DotSpawner : MonoBehaviour
{
    public GameObject dotPrefab;
    public TargetDetector detector;

    public PathData[] paths;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Spawn()
    {
        StartCoroutine(SpawnDot());
    }

    IEnumerator SpawnDot()
    {
        while (true)
        {
            float randomX = Random.Range(BoundingBox.Instance.min.x, BoundingBox.Instance.max.x);
            float randomY = Random.Range(BoundingBox.Instance.min.y, BoundingBox.Instance.max.y);

            Vector3 randomPos = new Vector3(randomX, randomY, dotPrefab.transform.position.z);
            GameObject instance = Instantiate(dotPrefab, randomPos, Quaternion.identity);
            DotBehaviour dot = instance.GetComponent<DotBehaviour>();
            
            PathData selectedPath = paths[Random.Range(0, paths.Length)];
            detector.AddDot(dot);
            dot.Path = selectedPath;
            dot.IsTrackable = true;

            //sumar tiempo que se demora en tocar
            yield return new WaitForSeconds(selectedPath.duration);
        }
    }
}
