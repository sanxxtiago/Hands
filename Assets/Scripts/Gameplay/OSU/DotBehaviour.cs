using UnityEngine;

public class DotBehaviour : MonoBehaviour
{
    public float lifeTime = 3;
    public float radius = .5f;
    public bool IsHitted { get; set; }
    void Update()
    {
        
    }

    public void Hit()
    {
        Debug.Log("Hitted!");
    }
}