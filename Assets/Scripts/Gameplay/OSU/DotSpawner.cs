using System.Collections;
using UnityEngine;

public class DotSpawner : MonoBehaviour
{
    public GameObject dotPrefab;
    public TargetDetector detector;
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
            detector.AddDot(dot);
            yield return new WaitForSeconds(dot.lifeTime);
        }
    }
}
