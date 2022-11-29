using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Configuration))]
public class ConfigurationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var script = (Configuration)target;

        if (GUILayout.Button("Fill", GUILayout.Height(40)))
        {
            script.Fill();
        }

        if (GUILayout.Button("Validate", GUILayout.Height(40)))
        {
            script.Validate();
        }

        if (GUILayout.Button("Import CSV", GUILayout.Height(40)))
        {
            script.ImportCSV();
        }
    }
}
#endif
