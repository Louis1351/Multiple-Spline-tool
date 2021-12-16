using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(PipeMeshGenerator))]
public class PipeMeshGeneratorEditor : SplineEditor
{
    PipeMeshGenerator component = null;
    private SerializedProperty nbQuad;
    private SerializedProperty width;
    private SerializedProperty material;
    private SerializedProperty autoGenerate;

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
        base.OnInspectorGUI(component.transform);

        EditorGUI.BeginChangeCheck();
        serializedObject.Update();

        DisplayOptions();

        serializedObject.ApplyModifiedProperties();

        if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
        {
            if (autoGenerate.boolValue)
            {
                component.Generate();
            }
        }
    }

    private void OnSceneGUI()
    {
        base.OnSceneGUI(component.transform, ref component.segments, ref component.center);

        if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
        {
            if (autoGenerate.boolValue)
            {
                component.Generate();
            }
            Undo.RecordObject(target, "Changed Look Target");
        }
    }

    private void DisplayOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Pipe", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(nbQuad);
        EditorGUILayout.PropertyField(width);
        EditorGUILayout.PropertyField(material);

        if (!autoGenerate.boolValue && GUILayout.Button("Generate"))
        {
            component.Generate();
        }

        DisplayDebug();
        
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DisplayDebug()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(autoGenerate);

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    public override void Initialize()
    {
        base.Initialize();
        component = target as PipeMeshGenerator;

        nbQuad = serializedObject.FindProperty("nbQuad");
        width = serializedObject.FindProperty("width");
        material = serializedObject.FindProperty("material");

        autoGenerate = serializedObject.FindProperty("autoGenerate");
    }
}

