using System.Collections;
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
    private Event currentEvt = null;
    private float radiusHandle = 0.25f;
    private List<int> idPointSelects;
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
        // showPoints = EditorGUILayout.Foldout(showPoints, "Points");

        /* if (showPoints)
         {*/
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Edition", EditorStyles.boldLabel);

        int newSize = EditorGUILayout.IntField("Number of points", segments.arraySize);
        if (newSize != segments.arraySize)
            segments.arraySize = newSize;

        if (!circleShape.boolValue)
        {
            for (int i = 0; i < segments.arraySize; ++i)
            {
                if (close.boolValue && i == segments.arraySize - 1)
                    break;

                SerializedProperty item = segments.GetArrayElementAtIndex(i);
                SerializedProperty p1 = item.FindPropertyRelative("p1");
                SerializedProperty p2 = item.FindPropertyRelative("p2");

                if (idPointSelects.Contains(-1) && i == 0)
                {
                    p1.vector3Value = EditorGUILayout.Vector3Field("point " + i, p1.vector3Value);
                }
                if (idPointSelects.Contains(i))
                {
                    p2.vector3Value = EditorGUILayout.Vector3Field("point " + (i + 1), p2.vector3Value);
                }
            }
        }
        else
        {
            EditorGUILayout.PropertyField(center);
        }

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        //}
    }

    private void OnSceneGUI()
    {
        EditorGUI.BeginChangeCheck();

        currentEvt = Event.current;

        // You'll need a control id to avoid messing with other tools!
        var controlID = GUIUtility.GetControlID(FocusType.Passive);
        var eventType = currentEvt.GetTypeForControl(controlID);

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

        if (currentEvt.control && currentEvt.button == 0)
        {
            if (!RemovePoint())
            {
                if (currentEvt.type == EventType.MouseDown)
                    AddPoint();
            }
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
        for (int i = 0; i < component.Segments.Length; ++i)
        {

            if (i == 0)
            {
                newP1 = DrawHandle(-1, i.ToString(), component.Segments[i].p1, Quaternion.identity);

                component.Segments[i].p1 = newP1;
                firstP = newP1;
            }
            else
            {
                newP1 = LastP;
                if (i >= component.Segments.Length) break;
                component.Segments[i].p1 = newP1;
            }

            if (close.boolValue && i == segments.arraySize - 1)
            {
                newP2 = firstP;
            }
            else
            {
                newP2 = DrawHandle(i, (i + 1).ToString(), component.Segments[i].p2, Quaternion.identity);
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


    private Vector3 DrawHandle(int id, string name, Vector3 position, Quaternion rotation)
    {
        Vector3 newPos = position;
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 15;
        style.alignment = TextAnchor.MiddleCenter;

        if (currentEvt.modifiers != EventModifiers.Control &&
        Handles.Button(position, Quaternion.identity, radiusHandle, radiusHandle, Handles.SphereHandleCap))
        {
            if ((circleShape.boolValue && id == -1) || !circleShape.boolValue)
            {
                SelectPoint(id);
            }
        }

        if (idPointSelects.Contains(id))
        {
            newPos = Handles.PositionHandle(position, Quaternion.identity);
        }
        else
        {
            if ((circleShape.boolValue && id == -1) || !circleShape.boolValue)
            {
                Handles.SphereHandleCap(0, position, Quaternion.identity, radiusHandle, EventType.Repaint);
            }
        }

        Handles.Label(newPos + Vector3.up * radiusHandle * 2.0f, name, style);

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

        newCenter = DrawHandle(-1, "center", component.Center, Quaternion.identity);//Handles.PositionHandle(component.Center, Quaternion.identity);
        component.Center = newCenter;

        float dist = 0.0f;

        for (int i = 0; i < component.Segments.Length; ++i)
        {
            if (i == 0)
            {
                newP1 = DrawHandle(i, i.ToString(), newCenter + eulerRot * new Vector3(Mathf.Cos(DivPI2 * i), Mathf.Sin(DivPI2 * i)) * _radius, Quaternion.identity);

                component.Segments[i].p1 = newP1;
                firstP = newP1;
            }
            else
            {
                newP1 = LastP;
                if (i >= component.Segments.Length) break;
                component.Segments[i].p1 = newP1;
            }

            if (close.boolValue && i == segments.arraySize - 1)
            {
                newP2 = firstP;
            }
            else
            {
                newP2 = DrawHandle(i, (i + 1).ToString(), newCenter + eulerRot * new Vector3(Mathf.Cos(DivPI2 * (i + 1)), Mathf.Sin(DivPI2 * (i + 1))) * _radius, Quaternion.identity);
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

        if (idPointSelects == null)
        {
            idPointSelects = new List<int>();
        }
        else
        {
            idPointSelects.Clear();
        }

    }
    private void DisplayCatmullRomSpline()
    {
        //Draw the Catmull-Rom spline between the points
        for (int i = 0; i < component.Segments.Length; ++i)
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

        ShowPoints();

        if (circleShape.boolValue)
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
        Ray r = HandleUtility.GUIPointToWorldRay(currentEvt.mousePosition);

        float dist = (component.Segments[component.Segments.Length - 1].p2 - r.origin).magnitude;

        Vector3 position = r.origin + r.direction * dist;

        int length = component.Segments.Length;

        Movable3DObject.Segment[] tmpArray = new Movable3DObject.Segment[length];
        System.Array.Copy(component.Segments, tmpArray, length);

        component.Segments = new Movable3DObject.Segment[length + 1];

        for (int i = 0; i < component.Segments.Length; ++i)
        {
            if (i == component.Segments.Length - 1)
            {
                component.Segments[i] = new Movable3DObject.Segment();
                if (close.boolValue)
                {
                    component.Segments[i].p1 = position;
                    component.Segments[i].p2 = component.Segments[0].p1;

                    component.Segments[i - 1].p2 = position;
                }
                else
                {
                    component.Segments[i].p1 = component.Segments[i - 1].p2;
                    component.Segments[i].p2 = position;
                }
            }
            else component.Segments[i] = new Movable3DObject.Segment(tmpArray[i]);
        }

        Repaint();
    }

    private bool RemovePoint()
    {
        idPointSelects.Clear();
        for (int i = 0; i < component.Segments.Length; ++i)
        {
            if (Handles.Button(component.Segments[i].p2, Quaternion.identity, radiusHandle, radiusHandle, Handles.SphereHandleCap)
                || Handles.Button(component.Segments[i].p1, Quaternion.identity, radiusHandle, radiusHandle, Handles.SphereHandleCap))
            {
                RemovePoint(i);
                return true;
            }
        }
        return false;
    }

    private void RemovePoint(int id)
    {
        int length = component.Segments.Length;
        int indexTmp = 0;
        Movable3DObject.Segment[] tmpArray = new Movable3DObject.Segment[length];
        System.Array.Copy(component.Segments, tmpArray, length);

        component.Segments = new Movable3DObject.Segment[length - 1];

        for (int i = 0; i < component.Segments.Length; ++i)
        {
            if (i == id)
            {
                indexTmp++;
                component.Segments[i] = new Movable3DObject.Segment(tmpArray[indexTmp]);

                if ((i - 1) >= 0)
                {
                    component.Segments[i - 1].p2 = component.Segments[i].p1;
                }
            }
            else
            {
                component.Segments[i] = new Movable3DObject.Segment(tmpArray[indexTmp]);
            }
            indexTmp++;
        }

        Repaint();
    }

    private void SelectPoint(int id)
    {
        if (!currentEvt.shift)
            idPointSelects.Clear();

        if (!idPointSelects.Contains(id))
        {
            idPointSelects.Add(id);
            Repaint();
        }
    }

    public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1,
        Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {

        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        //is coplanar, and not parallel
        if (Mathf.Abs(planarFactor) < 0.0001f
                && crossVec1and2.sqrMagnitude > 0.0001f)
        {
            float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
            intersection = linePoint1 + (lineVec1 * s);
            return true;
        }
        else
        {
            intersection = Vector3.zero;
            return false;
        }
    }

}
