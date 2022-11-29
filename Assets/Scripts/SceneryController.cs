using System.Collections;
using UnityEngine;

public class SceneryController : MonoBehaviour
{
    public float width;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireCube(
            new Vector3(transform.position.x, transform.position.x + 3.375f, transform.position.z), 
            new Vector3(5.2f, 6.75f, width));
    }
}