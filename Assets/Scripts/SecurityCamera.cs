using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SecurityCamera : MonoBehaviour
{
    [SerializeField] Material material;
    [SerializeField] List<Transform> nodes;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        if (nodes.Count >= 4 && !nodes.Any(x => x == null)) 
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var currentNode = nodes[i].position;
                var previousNode = Vector3.zero;

                if (i > 0)
                {
                    previousNode = nodes[i - 1].position;
                }
                else if (i == 0 && nodes.Count > 1)
                {
                    previousNode = nodes[nodes.Count - 1].position;
                }

                //if (previousNode != nodes[nodes.Count - 1].position)
                Gizmos.DrawLine(previousNode, currentNode);

                Gizmos.DrawWireSphere(currentNode, 0.3f);
            }
        }
    }

    private void Start()
    {
        var points = new List<Vector2>();

        foreach (Transform t in nodes)
            points.Add(new Vector2(t.localPosition.x, t.localPosition.y));

        var vertices2D = points.ToArray();
        var vertices3D = System.Array.ConvertAll<Vector2, Vector3>(vertices2D, v => v);

        var triangulator = new Triangulator(vertices2D);
        var indices = triangulator.Triangulate();

        // Create the mesh
        var mesh = new Mesh
        {
            vertices = vertices3D,
            triangles = indices
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        Vector2[] uv = new Vector2[] { new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0) };
        int[] indeces = new int[] { 0, 1, 3, 1, 2, 3};

        mesh.uv = uv;
        mesh.triangles = indeces;

        var meshCollider = gameObject.AddComponent<MeshCollider>();

        meshCollider.sharedMesh = mesh;

        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;

        var filter = gameObject.AddComponent<MeshFilter>();
        filter.mesh = mesh;
    }
}
