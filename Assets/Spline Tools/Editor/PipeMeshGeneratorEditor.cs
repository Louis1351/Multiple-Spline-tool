using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(PipeMeshGenerator))]
public class PipeMeshGeneratorEditor : SplineEditor
{
    private PipeMeshGenerator componentTarget;
    private SerializedProperty nbQuad;
    private SerializedProperty width;
    private SerializedProperty materials;
    private SerializedProperty catmullRollStep;
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
        EditorGUI.BeginChangeCheck();
        serializedObject.Update();

        base.OnInspectorGUI();

        DisplayOptions();

        serializedObject.ApplyModifiedProperties();

        if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
        {
            if (autoGenerate.boolValue)
            {
                if (!useCatmullRom.boolValue)
                {
                    componentTarget.Generate(componentTarget.segments);
                }
                else
                {
                    componentTarget.Generate();
                }

                Undo.RecordObject(target, "Changed Properties");
            }
        }
    }

    public override void OnSceneGUI()
    {
        base.OnSceneGUI();

        if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
        {
            if (autoGenerate.boolValue)
            {
                if (!useCatmullRom.boolValue)
                {
                    componentTarget.Generate(componentTarget.segments);
                }
                else
                {
                    componentTarget.Generate();
                }
            }
            Undo.RecordObject(target, "Changed Properties");
        }
    }

    private void DisplayOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Pipe", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Geometry", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(nbQuad);
        if (useCatmullRom.boolValue)
            EditorGUILayout.PropertyField(catmullRollStep);
        EditorGUILayout.PropertyField(width);


        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(materials);



        if (!autoGenerate.boolValue && GUILayout.Button("Generate"))
        {
            if (!useCatmullRom.boolValue)
            {
                componentTarget.Generate(componentTarget.segments);
            }
            else
            {
                componentTarget.Generate();
            }
        }

        if (GUILayout.Button("Save Mesh"))
        {
            componentTarget.SaveMesh();
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
        componentTarget = target as PipeMeshGenerator;

        nbQuad = serializedObject.FindProperty("nbQuad");
        width = serializedObject.FindProperty("width");
        materials = serializedObject.FindProperty("materials");

        catmullRollStep = serializedObject.FindProperty("catmullRollStep");
        autoGenerate = serializedObject.FindProperty("autoGenerate");
    }
}

