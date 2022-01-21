using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[CanEditMultipleObjects]
[CustomEditor(typeof(Spawnable3DObject))]
public class Spawnable3DObjectEditor : SplineEditor
{
    private Spawnable3DObject componentTarget;
    private SerializedProperty spawnableObjects;
    private SerializedProperty step;
    private SerializedProperty useDirection;
    private SerializedProperty autoGenerate;
    private SerializedProperty adaptToSurface;
    private SerializedProperty distanceDetection;
    private SerializedProperty layers;

    private SerializedProperty randomOrder;
    private SerializedProperty randomOffset;
    private SerializedProperty offsetAxis;
    private SerializedProperty distanceOffset;

    private SerializedProperty scaleType;
    private SerializedProperty currentScale;
    private SerializedProperty minScale;
    private SerializedProperty maxScale;
    private SerializedProperty curvedScale;

    private SerializedProperty rotationType;
    private SerializedProperty currentRotation;
    private SerializedProperty minRotation;
    private SerializedProperty maxRotation;
    private SerializedProperty curvedRotation;
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
                componentTarget.GenerateObjects();
            }
            Undo.RecordObject(target, "Changed Properties");
        }
    }

    public override void OnSceneGUI()
    {
        base.OnSceneGUI();

        if (adaptToSurface.boolValue)
            DisplayRadiusSurfaceDetection();

        if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
        {
            if (autoGenerate.boolValue)
            {
                componentTarget.GenerateObjects();
            }
            Undo.RecordObject(target, "Changed Properties");
        }
    }

    private void DisplayOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Spawn Options", EditorStyles.boldLabel);


        DisplayObjects();
        DisplayTransform();

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        if (!autoGenerate.boolValue && GUILayout.Button("Instantiate Objects"))
        {
            componentTarget.GenerateObjects();
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
    private void DisplayObjects()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Objects", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(randomOrder);
        EditorGUILayout.PropertyField(spawnableObjects);

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DisplayPosition()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(step);

        EditorGUILayout.PropertyField(randomOffset);
        if (randomOffset.boolValue)
        {
            EditorGUILayout.PropertyField(distanceOffset);
            EditorGUILayout.PropertyField(offsetAxis);
        }

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DisplayRotation()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useDirection);

        EditorGUILayout.PropertyField(rotationType);

        switch ((Spawnable3DObject.RotationType)rotationType.enumValueIndex)
        {
            case Spawnable3DObject.RotationType.none:
                EditorGUILayout.PropertyField(currentRotation);
                break;
            case Spawnable3DObject.RotationType.linear:
                EditorGUILayout.PropertyField(minRotation);
                EditorGUILayout.PropertyField(maxRotation);
                break;
            case Spawnable3DObject.RotationType.randomByAxis:
                EditorGUILayout.PropertyField(minRotation);
                EditorGUILayout.PropertyField(maxRotation);
                break;
            case Spawnable3DObject.RotationType.randomBetweenTwoConstant:
                EditorGUILayout.PropertyField(minRotation);
                EditorGUILayout.PropertyField(maxRotation);
                break;
            case Spawnable3DObject.RotationType.curved:
                EditorGUILayout.PropertyField(curvedRotation);
                break;

        }

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
                EditorGUILayout.PropertyField(currentScale);
                break;
            case Spawnable3DObject.ScaleType.linear:
                EditorGUILayout.PropertyField(minScale);
                EditorGUILayout.PropertyField(maxScale);
                break;
            case Spawnable3DObject.ScaleType.randomByAxis:
                EditorGUILayout.PropertyField(minScale);
                EditorGUILayout.PropertyField(maxScale);
                break;
            case Spawnable3DObject.ScaleType.randomBetweenTwoConstant:
                EditorGUILayout.PropertyField(minScale);
                EditorGUILayout.PropertyField(maxScale);
                break;
            case Spawnable3DObject.ScaleType.curved:
                EditorGUILayout.PropertyField(curvedScale);
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
            Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, 0.3f);
            Handles.CircleHandleCap(0, p1.vector3Value, Quaternion.LookRotation(fwd, Vector3.up), distanceDetection.floatValue, EventType.Repaint);
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        componentTarget = target as Spawnable3DObject;

        spawnableObjects = serializedObject.FindProperty("spawnableObjects");
        step = serializedObject.FindProperty("step");
        useDirection = serializedObject.FindProperty("useDirection");

        adaptToSurface = serializedObject.FindProperty("adaptToSurface");
        distanceDetection = serializedObject.FindProperty("distanceDetection");
        layers = serializedObject.FindProperty("layers");

        randomOrder = serializedObject.FindProperty("randomOrder");
        randomOffset = serializedObject.FindProperty("randomOffset");
        offsetAxis = serializedObject.FindProperty("offsetAxis");
        distanceOffset = serializedObject.FindProperty("distanceOffset");

        scaleType = serializedObject.FindProperty("scaleType");
        currentScale = serializedObject.FindProperty("currentScale");
        minScale = serializedObject.FindProperty("minScale");
        maxScale = serializedObject.FindProperty("maxScale");
        curvedScale = serializedObject.FindProperty("curvedScale");

        rotationType = serializedObject.FindProperty("rotationType");
        currentRotation = serializedObject.FindProperty("currentRotation");
        minRotation = serializedObject.FindProperty("minRotation");
        maxRotation = serializedObject.FindProperty("maxRotation");
        curvedRotation = serializedObject.FindProperty("curvedRotation");

        autoGenerate = serializedObject.FindProperty("autoGenerate");
    }
}
