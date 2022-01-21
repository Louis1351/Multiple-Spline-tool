using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[CanEditMultipleObjects]
[CustomEditor(typeof(Movable3DObject))]
public class Movable3DObjectEditor : SplineEditor
{
    private Movable3DObject componentTarget;
    private SerializedProperty type;
    private SerializedProperty isMovingOnStart;
    private SerializedProperty isChangingDirection;
    private SerializedProperty startingPos;
    private SerializedProperty speed;
    private SerializedProperty useCurvedSpeed;
    private SerializedProperty curve;

    private SerializedProperty isReversed;
    private SerializedProperty startMovement;
    private SerializedProperty endMovement;

    private Movable3DObject.MovementType typeEnum;
    private SerializedProperty loop;

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
            currentDist.floatValue = componentTarget.GetCurrentDistance(startingPos.floatValue);

            if (isChangingDirection.boolValue)
            {
                componentTarget.transform.forward = componentTarget.CurrentDir;
            }
            Undo.RecordObject(target, "Changed Properties");
        }
    }

    public override void OnSceneGUI()
    {
        base.OnSceneGUI();

        if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
        {
            currentDist.floatValue = componentTarget.GetCurrentDistance(startingPos.floatValue);
            Undo.RecordObject(target, "Changed Properties");
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        componentTarget = (Movable3DObject)target;

        type = serializedObject.FindProperty("type");
        isMovingOnStart = serializedObject.FindProperty("isMovingOnStart");
        isChangingDirection = serializedObject.FindProperty("isChangingDirection");
        startingPos = serializedObject.FindProperty("startingPos");

        speed = serializedObject.FindProperty("speed");
        useCurvedSpeed = serializedObject.FindProperty("useCurvedSpeed");
        curve = serializedObject.FindProperty("curve");
        isReversed = serializedObject.FindProperty("isReversed");
        startMovement = serializedObject.FindProperty("startMovement");
        endMovement = serializedObject.FindProperty("endMovement");
        loop = serializedObject.FindProperty("loop");
    }


    private void DisplayOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        DisplayMovement();
        DisplayVelocity();
        DisplayTransformOptions();
        DisplayEvents();

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DisplayTransformOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(startingPos);

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(isChangingDirection);

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DisplayMovement()
    {
        typeEnum = (Movable3DObject.MovementType)type.enumValueIndex;

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Movement Options", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(type);

        if (typeEnum != Movable3DObject.MovementType.PingPong)
        {
            EditorGUILayout.PropertyField(loop);
        }
        else
        {
            loop.boolValue = false;
        }

        EditorGUILayout.PropertyField(isMovingOnStart);
        EditorGUILayout.PropertyField(isReversed);

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DisplayVelocity()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Velocity", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(useCurvedSpeed);

        if (useCurvedSpeed.boolValue)
        {
            EditorGUILayout.PropertyField(curve);
        }
        else
        {
            EditorGUILayout.PropertyField(speed);
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DisplayEvents()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(startMovement);
        EditorGUILayout.PropertyField(endMovement);

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }
}
