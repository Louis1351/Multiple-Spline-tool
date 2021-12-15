using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(PipeMeshGenerator))]
public class PipeMeshGeneratorEditor : Editor
{
    PipeMeshGenerator component = null;
    private SerializedProperty center;
    private SerializedProperty demiCircle;
    private SerializedProperty currentDist;
    private SerializedProperty radius;
    private SerializedProperty rotation;
    private SerializedProperty segments;
    private SerializedProperty useCatmullRom;
    private SerializedProperty loop;
    private SerializedProperty close;
    private bool oldClose;
    private bool showPoints = false;



    private SerializedProperty nbQuad;
    private SerializedProperty width;
    private SerializedProperty material;

    void OnAwake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        oldClose = close.boolValue;
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(nbQuad);
        EditorGUILayout.PropertyField(width);
        EditorGUILayout.PropertyField(material);

        int oldSizeArray = segments.arraySize;
        serializedObject.Update();

        EditorGUILayout.PropertyField(useCatmullRom);
        EditorGUILayout.PropertyField(loop);

        EditorGUILayout.PropertyField(close);
        EditorGUILayout.PropertyField(radius);

        if (radius.floatValue > 0.0f)
        {
            EditorGUILayout.PropertyField(demiCircle);
            EditorGUILayout.PropertyField(rotation);
        }

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

        if (GUILayout.Button("Generate"))
        {
            component.Generate();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI()
    {
        EditorGUI.BeginChangeCheck();
        if (segments.arraySize > 0)
            Tools.current = Tool.None;

        float radiusValue = radius.floatValue;

        if (useCatmullRom.boolValue)
        {
            DisplayCatmullRomSpline();
        }
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
        Quaternion rot = Quaternion.Euler(rotation.vector3Value.x, rotation.vector3Value.y, rotation.vector3Value.z);

        for (int i = 0; i < segments.arraySize; ++i)
        {
            if (i == 0)
            {
                newP1 = newCenter + rot * new Vector3(Mathf.Cos(DivPI2 * i), Mathf.Sin(DivPI2 * i)) * _radius;
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
                newP2 = newCenter + rot * new Vector3(Mathf.Cos(DivPI2 * (i + 1)), Mathf.Sin(DivPI2 * (i + 1))) * _radius;
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
        component = target as PipeMeshGenerator;

        currentDist = serializedObject.FindProperty("currentDist");
        center = serializedObject.FindProperty("center");
        demiCircle = serializedObject.FindProperty("demiCircle");
        radius = serializedObject.FindProperty("radius");
        rotation = serializedObject.FindProperty("rotation");
        segments = serializedObject.FindProperty("segments");

        useCatmullRom = serializedObject.FindProperty("useCatmullRom");
        loop = serializedObject.FindProperty("loop");
        close = serializedObject.FindProperty("close");



        nbQuad = serializedObject.FindProperty("nbQuad");
        width = serializedObject.FindProperty("width");
        material = serializedObject.FindProperty("material");
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
}

