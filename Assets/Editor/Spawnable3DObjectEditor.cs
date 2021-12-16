using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[CanEditMultipleObjects]
[CustomEditor(typeof(Spawnable3DObject))]
public class Spawnable3DObjectEditor : SplineEditor
{
    private Spawnable3DObject component;
    private SerializedProperty spawnableObjects;
    private SerializedProperty step;
    private SerializedProperty useDirection;
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

        base.OnInspectorGUI(component.transform);

        DisplayOptions();

        serializedObject.ApplyModifiedProperties();

        if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
        {
            if (autoGenerate.boolValue)
            {
                component.GenerateObjects();
            }
            Undo.RecordObject(target, "Changed Properties");
        }
    }

    private void OnSceneGUI()
    {
        base.OnSceneGUI(component.transform, ref component.segments, ref component.center);

        if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
        {
            if (autoGenerate.boolValue)
            {
                component.GenerateObjects();
            }
            Undo.RecordObject(target, "Changed Properties");
        }
    }

    private void DisplayOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Spawn Options", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(spawnableObjects);

        DisplayTransform();

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        if (!autoGenerate.boolValue && GUILayout.Button("Instantiate Objects"))
        {
            component.GenerateObjects();
        }

        DisplayDebug();

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DisplayTransform()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");
        EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(step);

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useDirection);

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
        component = target as Spawnable3DObject;

        spawnableObjects = serializedObject.FindProperty("spawnableObjects");
        step = serializedObject.FindProperty("step");
        useDirection = serializedObject.FindProperty("useDirection");

        autoGenerate = serializedObject.FindProperty("autoGenerate");
    }
}
