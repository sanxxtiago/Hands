using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prueba : MonoBehaviour
{
    public Quaternion rotation;

    public bool globalRotation = false;
    void Update()
    {
        if (globalRotation)
        {
            transform.rotation = rotation * transform.rotation;
        }
        else
        {
            transform.rotation = transform.rotation * rotation;
        }
    }
}
