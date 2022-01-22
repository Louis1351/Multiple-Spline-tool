using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(SimpleSpline))]
public class SimpleSplineEditor : SplineEditor
{
   void OnAwake()
    {
        Initialize();
    }

    public override void OnEnable()
    {
        Initialize();
        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        serializedObject.Update();

        base.OnInspectorGUI();

        serializedObject.ApplyModifiedProperties();
    }

    public override void OnSceneGUI()
    {
        base.OnSceneGUI();
    }

    public override void Initialize()
    {
        base.Initialize();
    }
}
