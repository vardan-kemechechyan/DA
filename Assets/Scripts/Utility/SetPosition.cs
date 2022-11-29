using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetPosition : MonoBehaviour
{
    [SerializeField] Transform target;

    Transform t;

    void Start()
    {
        t = GetComponent<Transform>();
    }

    void Update()
    {
        if (target && t)
            t.position = target.position;
    }
}
