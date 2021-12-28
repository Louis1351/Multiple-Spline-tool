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
    private SerializedProperty adaptToSurface;
    private SerializedProperty distanceDetection;
    private SerializedProperty layers;
    private SerializedProperty scaleType;
    private SerializedProperty scale;
    private SerializedProperty minScale;
    private SerializedProperty maxScale;

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

        if (adaptToSurface.boolValue)
            DisplayRadiusSurfaceDetection();

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

        DisplaySurface();
        DisplayPosition();
        DisplayRotation();
        DisplayScale();
    }

    private void DisplaySurface()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Surface", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(adaptToSurface);

        if (adaptToSurface.boolValue)
        {
            EditorGUILayout.PropertyField(distanceDetection);
            EditorGUILayout.PropertyField(layers);
        }

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DisplayPosition()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(step);

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DisplayRotation()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useDirection);

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DisplayScale()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Scale", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(scaleType);

        switch ((Spawnable3DObject.ScaleType)scaleType.enumValueIndex)
        {
            case Spawnable3DObject.ScaleType.none:
                EditorGUILayout.PropertyField(scale);
                break;
            case Spawnable3DObject.ScaleType.linear:
                EditorGUILayout.PropertyField(minScale);
                EditorGUILayout.PropertyField(maxScale);
                break;
            case Spawnable3DObject.ScaleType.random:
                EditorGUILayout.PropertyField(minScale);
                EditorGUILayout.PropertyField(maxScale);
                break;
            case Spawnable3DObject.ScaleType.curved:
                break;

        }

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

    private void DisplayRadiusSurfaceDetection()
    {
        SerializedProperty item;
        SerializedProperty p1;
        for (int i = 0; i < segments.arraySize; i++)
        {
            item = segments.GetArrayElementAtIndex(i);
            p1 = item.FindPropertyRelative("p1");
            Vector3 fwd = p1.vector3Value - Camera.current.transform.position;
            Handles.CircleHandleCap(0, p1.vector3Value, Quaternion.LookRotation(fwd, Vector3.up), distanceDetection.floatValue, EventType.Repaint);
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        component = target as Spawnable3DObject;

        spawnableObjects = serializedObject.FindProperty("spawnableObjects");
        step = serializedObject.FindProperty("step");
        useDirection = serializedObject.FindProperty("useDirection");

        adaptToSurface = serializedObject.FindProperty("adaptToSurface");
        distanceDetection = serializedObject.FindProperty("distanceDetection");
        layers = serializedObject.FindProperty("layers");

        scaleType = serializedObject.FindProperty("scaleType");
        scale = serializedObject.FindProperty("scale");
        minScale = serializedObject.FindProperty("minScale");
        maxScale = serializedObject.FindProperty("maxScale");

        autoGenerate = serializedObject.FindProperty("autoGenerate");
    }
}
