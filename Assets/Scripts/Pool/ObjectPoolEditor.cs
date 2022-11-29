using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObjectPool))]
public class ObjectPoolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var script = (ObjectPool)target;

        if (GUILayout.Button("Fill", GUILayout.Height(40)))
        {
            script.Fill();
        }
    }
}
#endif