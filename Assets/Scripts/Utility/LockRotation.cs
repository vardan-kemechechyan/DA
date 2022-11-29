using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockRotation : MonoBehaviour
{
    Transform t;
    Quaternion rot;

    void Start()
    {
        t = GetComponent<Transform>();
        rot = transform.rotation;
    }

    void Update()
    {
        t.rotation = rot;
    }
}
