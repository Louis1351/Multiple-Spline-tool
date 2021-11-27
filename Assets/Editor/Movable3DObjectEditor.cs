﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[CanEditMultipleObjects]
[CustomEditor(typeof(Movable3DObject))]
public class Movable3DObjectEditor : Editor
{
    private Movable3DObject component;
    private SerializedProperty type;
    private SerializedProperty isMovingOnStart;
    private SerializedProperty isChangingDirection;
    private SerializedProperty startingPos;
    private SerializedProperty center;
    private SerializedProperty demiCircle;
    private SerializedProperty rotation;
    private SerializedProperty currentDist;
    private SerializedProperty radius;
    private SerializedProperty segments;
    private SerializedProperty speed;
    private SerializedProperty useCurvedSpeed;
    private SerializedProperty curve;
    private SerializedProperty useCatmullRom;
    private SerializedProperty loop;
    private SerializedProperty close;
    private SerializedProperty isReversed;
    private SerializedProperty startMovement;
    private SerializedProperty endMovement;
    private SerializedProperty circleShape;
    private bool oldClose;
    private Movable3DObject.MovementType typeEnum;
    private bool showPoints = false;
    void OnAwake()
    {
        Initialize();
    }

    void OnEnable()
    {
        Initialize();
        oldClose = close.boolValue;
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        int oldSizeArray = segments.arraySize;
        serializedObject.Update();
        typeEnum = (Movable3DObject.MovementType)type.enumValueIndex;

        DisplayMovementOptions();

        EditorGUILayout.Space();

        DisplaySplineOptions();

        EditorGUILayout.Space();

        DisplayEvents();

        if (oldSizeArray == 0 && segments.arraySize != oldSizeArray)
        {
            for (int i = 0; i < segments.arraySize; i++)
            {
                SerializedProperty item = segments.GetArrayElementAtIndex(i);
                SerializedProperty p1 = item.FindPropertyRelative("p1");
                SerializedProperty p2 = item.FindPropertyRelative("p2");
                p1.vector3Value = component.transform.position;
                p2.vector3Value = component.transform.position;
            }
        }

        serializedObject.ApplyModifiedProperties();

        if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
        {
            currentDist.floatValue = component.GetCurrentDistance(startingPos.floatValue);

            if (isChangingDirection.boolValue)
            {
                component.transform.forward = component.CurrentDir;
            }
        }
    }

    private void ShowPoints()
    {
        showPoints = EditorGUILayout.Foldout(showPoints, "Points");
        if (showPoints)
        {

            for (int i = 0; i < segments.arraySize; ++i)
            {
                if (close.boolValue && i == segments.arraySize - 1)
                    break;

                SerializedProperty item = segments.GetArrayElementAtIndex(i);
                SerializedProperty p1 = item.FindPropertyRelative("p1");
                SerializedProperty p2 = item.FindPropertyRelative("p2");
                if (i == 0)
                {
                    p1.vector3Value = EditorGUILayout.Vector3Field("point " + i, p1.vector3Value);
                    p2.vector3Value = EditorGUILayout.Vector3Field("point " + (i + 1), p2.vector3Value);
                }
                else
                {
                    p2.vector3Value = EditorGUILayout.Vector3Field("point " + (i + 1), p2.vector3Value);
                }
            }
        }
    }

    private void OnSceneGUI()
    {
        EditorGUI.BeginChangeCheck();
        if (segments.arraySize > 0)
            Tools.current = Tool.None;


        if (useCatmullRom.boolValue)
        {
            DisplayCatmullRomSpline();
        }
        if (!circleShape.boolValue)
        {
            SetFreeSegments();
        }
        else
        {
            SetCircleSegments(radius.floatValue);
        }
        if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
        {
            currentDist.floatValue = component.GetCurrentDistance(startingPos.floatValue);
            Undo.RecordObject(target, "Changed Look Target");
        }
    }

    private void SetFreeSegments()
    {
        Vector3 firstP = Vector3.zero;
        Vector3 LastP = Vector3.zero;
        Vector3 newP1 = Vector3.zero;
        Vector3 newP2 = Vector3.zero;

        float dist = 0.0f;

        for (int i = 0; i < segments.arraySize; ++i)
        {
            if (i == 0)
            {
                newP1 = DrawHandle(i.ToString(), component.Segments[i].p1, Quaternion.identity);

                component.Segments[i].p1 = newP1;
                firstP = newP1;
            }
            else
            {
                newP1 = LastP;
                component.Segments[i].p1 = newP1;
            }

            if (close.boolValue && i == segments.arraySize - 1)
            {
                newP2 = firstP;
            }
            else
            {
                newP2 = DrawHandle((i + 1).ToString(), component.Segments[i].p2, Quaternion.identity);
            }

            component.Segments[i].p2 = newP2;

            component.Segments[i].p1length = dist;

            component.Segments[i].length = (newP2 - newP1).magnitude;

            dist += component.Segments[i].length;

            component.Segments[i].p2length = dist;

            component.Segments[i].dir = (newP2 - newP1).normalized;

            Handles.DrawDottedLine(newP1, newP2, 5.0f);

            LastP = newP2;
        }
    }

    private Vector3 DrawHandle(string name, Vector3 position, Quaternion rotation)
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 15;
        style.alignment = TextAnchor.MiddleCenter;
        Vector3 newPos = Handles.PositionHandle(position, Quaternion.identity);
        Handles.Label(newPos, name, style);
        return newPos;
    }

    private void SetCircleSegments(float _radius)
    {
        Vector3 firstP = Vector3.zero;
        Vector3 LastP = Vector3.zero;
        Vector3 newP1 = Vector3.zero;
        Vector3 newP2 = Vector3.zero;
        Vector3 newCenter = Vector3.zero;

        float PI2 = Mathf.PI * ((demiCircle.boolValue) ? 1.0f : 2.0f);
        float DivPI2 = PI2 / ((close.boolValue) ? segments.arraySize : (segments.arraySize + 1));

        Quaternion eulerRot = Quaternion.Euler(rotation.vector3Value.x, rotation.vector3Value.y, rotation.vector3Value.z);

        newCenter = DrawHandle("center", component.Center, Quaternion.identity);//Handles.PositionHandle(component.Center, Quaternion.identity);
        component.Center = newCenter;

        float dist = 0.0f;

        for (int i = 0; i < segments.arraySize; ++i)
        {
            if (i == 0)
            {
                newP1 = DrawHandle(i.ToString(), newCenter + eulerRot * new Vector3(Mathf.Cos(DivPI2 * i), Mathf.Sin(DivPI2 * i)) * _radius, Quaternion.identity);

                component.Segments[i].p1 = newP1;
                firstP = newP1;
            }
            else
            {
                newP1 = LastP;
                component.Segments[i].p1 = newP1;
            }

            if (close.boolValue && i == segments.arraySize - 1)
            {
                newP2 = firstP;
            }
            else
            {
                newP2 = DrawHandle((i + 1).ToString(), newCenter + eulerRot * new Vector3(Mathf.Cos(DivPI2 * (i + 1)), Mathf.Sin(DivPI2 * (i + 1))) * _radius, Quaternion.identity);
            }

            component.Segments[i].p1length = dist;

            component.Segments[i].length = (newP2 - newP1).magnitude;

            component.Segments[i].p2 = newP2;

            dist += component.Segments[i].length;

            component.Segments[i].p2length = dist;

            component.Segments[i].dir = (newP2 - newP1).normalized;

            Handles.DrawDottedLine(newP1, newP2, 5.0f);

            LastP = newP2;
        }
    }

    private void Initialize()
    {
        component = (Movable3DObject)target;

        type = serializedObject.FindProperty("type");
        isMovingOnStart = serializedObject.FindProperty("isMovingOnStart");
        isChangingDirection = serializedObject.FindProperty("isChangingDirection");
        startingPos = serializedObject.FindProperty("startingPos");
        currentDist = serializedObject.FindProperty("currentDist");

        circleShape = serializedObject.FindProperty("circleShape");
        center = serializedObject.FindProperty("center");
        demiCircle = serializedObject.FindProperty("demiCircle");
        radius = serializedObject.FindProperty("radius");
        rotation = serializedObject.FindProperty("rotation");

        segments = serializedObject.FindProperty("segments");

        speed = serializedObject.FindProperty("speed");
        useCurvedSpeed = serializedObject.FindProperty("useCurvedSpeed");
        curve = serializedObject.FindProperty("curve");
        isReversed = serializedObject.FindProperty("isReversed");
        startMovement = serializedObject.FindProperty("startMovement");
        endMovement = serializedObject.FindProperty("endMovement");
        useCatmullRom = serializedObject.FindProperty("useCatmullRom");
        loop = serializedObject.FindProperty("loop");
        close = serializedObject.FindProperty("close");
    }
    private void DisplayCatmullRomSpline()
    {
        //Draw the Catmull-Rom spline between the points
        for (int i = 0; i < segments.arraySize; ++i)
        {
            DisplayCatmullRomSpline(i);
        }
    }

    private void DisplayCatmullRomSpline(int pos)
    {
        //The start position of the line
        Vector3 lastPos = component.Segments[component.ClampListPos(pos)].p1;

        //The spline's resolution
        //Make sure it's is adding up to 1, so 0.3 will give a gap, but 0.2 will work
        float resolution = 0.1f;

        //How many times should we loop?
        int loops = Mathf.FloorToInt(1f / resolution);
        Vector3 newPos = Vector3.zero;
        for (int i = 1; i <= loops; i++)
        {
            //Which t position are we at?
            float t = i * resolution;
            //Find the coordinate between the end points with a Catmull-Rom spline
            if (close.boolValue)
            {
                newPos = component.GetCatmullRomPosition(t,
             component.Segments[component.ClampListPos(pos - 1)].p1,
             component.Segments[component.ClampListPos(pos)].p1,
             component.Segments[component.ClampListPos(pos)].p2,
             component.Segments[component.ClampListPos(pos + 1)].p2);
            }
            else
            {
                if (pos == 0)
                {
                    newPos = component.GetCatmullRomPosition(t,
                component.Segments[component.ClampListPos(pos)].p1,
                component.Segments[component.ClampListPos(pos)].p1,
                component.Segments[component.ClampListPos(pos)].p2,
                component.Segments[component.ClampListPos(pos + 1)].p2);
                }
                else if (pos == segments.arraySize - 1)
                {
                    newPos = component.GetCatmullRomPosition(t,
                component.Segments[component.ClampListPos(pos - 1)].p1,
                component.Segments[component.ClampListPos(pos)].p1,
                component.Segments[component.ClampListPos(pos)].p2,
                component.Segments[component.ClampListPos(pos)].p2);
                }
                else
                {
                    newPos = component.GetCatmullRomPosition(t,
                   component.Segments[component.ClampListPos(pos - 1)].p1,
                   component.Segments[component.ClampListPos(pos)].p1,
                   component.Segments[component.ClampListPos(pos)].p2,
                   component.Segments[component.ClampListPos(pos + 1)].p2);
                }
            }

            //Draw this line segment
            Handles.color = Color.blue;
            Handles.DrawLine(lastPos, newPos);
            Handles.color = Color.white;
            //Save this pos so we can draw the next line segment
            lastPos = newPos;
        }
    }
    private void DisplayMovementOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(type);
        EditorGUILayout.PropertyField(isMovingOnStart);
        EditorGUILayout.PropertyField(isReversed);

        DisplayVelocity();
        DisplayTransformOptions();

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DisplayTransformOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(startingPos);
        EditorGUILayout.PropertyField(isChangingDirection);

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DisplaySplineOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Spline", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(useCatmullRom);
        if (typeEnum != Movable3DObject.MovementType.PingPong)
        {
            EditorGUILayout.PropertyField(loop);
        }
        else
        {
            loop.boolValue = false;
        }

        EditorGUILayout.PropertyField(close);
        EditorGUILayout.PropertyField(circleShape);

        int newSize = EditorGUILayout.IntField("Number of points", segments.arraySize);
        if (newSize != segments.arraySize)
            segments.arraySize = newSize;

        if (!circleShape.boolValue)
        {
            ShowPoints();
        }
        else
            DisplayCircleOptions();

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DisplayCircleOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Circle", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(radius);

        EditorGUILayout.PropertyField(demiCircle);
        EditorGUILayout.PropertyField(center);
        EditorGUILayout.PropertyField(rotation);

        if (close.boolValue)
        {
            if (oldClose != close.boolValue)
            {
                segments.arraySize += 1;
                oldClose = close.boolValue;
            }
        }

        if (oldClose != close.boolValue)
        {
            segments.arraySize -= 1;
            oldClose = close.boolValue;
        }

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
    private void AddPoint()
    {

    }

    private void RemovePoint()
    {

    }
}