using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[CanEditMultipleObjects]
[CustomEditor(typeof(MovableObject))]
public class MovableObjectEditor : Editor
{
    private MovableObject component;
    private SerializedProperty type;
    private SerializedProperty isMovingOnStart;
    private SerializedProperty isChangingDirection;
    private SerializedProperty startingPos;
    private SerializedProperty center;
    private SerializedProperty demiCircle;
    private SerializedProperty currentDist;
    private SerializedProperty radius;
    private SerializedProperty segments;
    private SerializedProperty speed;
    private SerializedProperty useCurvedSpeed;
    private SerializedProperty curve;
    private SerializedProperty loop;
    private SerializedProperty close;
    private SerializedProperty isReversed;
    private SerializedProperty startMovement;
    private SerializedProperty endMovement;
    private bool oldClose;
    private MovableObject.MovementType typeEnum;
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
        typeEnum = (MovableObject.MovementType)type.enumValueIndex;

        EditorGUILayout.PropertyField(type);

        EditorGUILayout.PropertyField(isMovingOnStart);
        if (typeEnum != MovableObject.MovementType.PingPong)
        {
            EditorGUILayout.PropertyField(loop);
        }
        else
        {
            loop.boolValue = false;
        }
        
        EditorGUILayout.PropertyField(close);
        EditorGUILayout.PropertyField(isChangingDirection);

        EditorGUILayout.PropertyField(startingPos);
        EditorGUILayout.PropertyField(radius);

        if (radius.floatValue > 0.0f)
            EditorGUILayout.PropertyField(demiCircle);

        int newsSize = EditorGUILayout.IntField("Number of points", segments.arraySize);

        if (newsSize != segments.arraySize)
            segments.arraySize = newsSize;

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

        if (radius.floatValue <= 0.0f)
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


        EditorGUILayout.PropertyField(useCurvedSpeed);

        if (useCurvedSpeed.boolValue)
        {
            EditorGUILayout.PropertyField(curve);
        }
        else
        {
            EditorGUILayout.PropertyField(speed);
        }

        EditorGUILayout.PropertyField(isReversed);
        EditorGUILayout.PropertyField(startMovement);
        EditorGUILayout.PropertyField(endMovement);

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

    private void OnSceneGUI()
    {
        EditorGUI.BeginChangeCheck();
        if (segments.arraySize > 0)
            Tools.current = Tool.None;

        float radiusValue = radius.floatValue;

        if (radiusValue <= 0.0f)
        {
            SetFreeSegments();
        }
        else
        {
            SetCircleSegments(radiusValue);
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
                newP1 = Handles.PositionHandle(component.Segments[i].p1, Quaternion.identity);
                Handles.Label(newP1, i.ToString());

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
                newP2 = Handles.PositionHandle(component.Segments[i].p2, Quaternion.identity);
                Handles.Label(newP2, (i + 1).ToString());
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

    private void SetCircleSegments(float _radius)
    {
        Vector3 firstP = Vector3.zero;
        Vector3 LastP = Vector3.zero;
        Vector3 newP1 = Vector3.zero;
        Vector3 newP2 = Vector3.zero;
        Vector3 newCenter = Vector3.zero;

        float PI2 = Mathf.PI * ((demiCircle.boolValue) ? 1.0f : 2.0f);
        float DivPI2 = PI2 / ((close.boolValue) ? segments.arraySize : (segments.arraySize + 1));

        newCenter = Handles.PositionHandle(component.Center, Quaternion.identity);
        component.Center = newCenter;

        float dist = 0.0f;

        for (int i = 0; i < segments.arraySize; ++i)
        {
            if (i == 0)
            {
                newP1 = newCenter + new Vector3(Mathf.Cos(DivPI2 * i), Mathf.Sin(DivPI2 * i)) * _radius;
                Handles.Label(newP1, i.ToString());

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
                newP2 = newCenter + new Vector3(Mathf.Cos(DivPI2 * (i + 1)), Mathf.Sin(DivPI2 * (i + 1))) * _radius;
                Handles.Label(newP2, (i + 1).ToString());
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
        component = (MovableObject)target;

        type = serializedObject.FindProperty("type");
        isMovingOnStart = serializedObject.FindProperty("isMovingOnStart");
        isChangingDirection = serializedObject.FindProperty("isChangingDirection");
        startingPos = serializedObject.FindProperty("startingPos");
        currentDist = serializedObject.FindProperty("currentDist");
        center = serializedObject.FindProperty("center");
        demiCircle = serializedObject.FindProperty("demiCircle");
        radius = serializedObject.FindProperty("radius");
        segments = serializedObject.FindProperty("segments");
        speed = serializedObject.FindProperty("speed");
        useCurvedSpeed = serializedObject.FindProperty("useCurvedSpeed");
        curve = serializedObject.FindProperty("curve");
        isReversed = serializedObject.FindProperty("isReversed");
        startMovement = serializedObject.FindProperty("startMovement");
        endMovement = serializedObject.FindProperty("endMovement");
        loop = serializedObject.FindProperty("loop");
        close = serializedObject.FindProperty("close");
    }
}
