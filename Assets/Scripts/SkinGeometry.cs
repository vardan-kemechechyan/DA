using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinGeometry : MonoBehaviour
{
    GameObject[] parts;

    private void Combine() 
    {
        if (parts != null)
            return;

        var p = new List<GameObject>();
        var geometry = transform.GetChild(1);

        foreach (Transform t in geometry)
            p.Add(t.gameObject);

        parts = p.ToArray();
    }

    public void Divide(bool divide) 
    {
        Combine();

        for (int x = 0; x < parts.Length; x++)
        {
            if (x == 0) parts[x].SetActive(!divide);
            else parts[x].SetActive(divide);
        }
    }
}