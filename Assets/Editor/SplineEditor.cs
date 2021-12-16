using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SplineEditor : Editor
{
    private SerializedProperty center;
    private SerializedProperty demiCircle;
    private SerializedProperty rotation;
    private SerializedProperty currentDist;
    private SerializedProperty radius;
    private SerializedProperty segments;

    private SerializedProperty useCatmullRom;
    private SerializedProperty close;

    private SerializedProperty circleShape;
    private bool oldClose;
    private Event currentEvt = null;
    private float radiusHandle = 0.25f;
    private List<int> idPointSelects;

    public SerializedProperty CurrentDist { get => currentDist; set => currentDist = value; }

    //public Transform trComp = null;
    // public Segment[] segmentsComp = null;
    public virtual void OnEnable()
    {
        oldClose = close.boolValue;
    }

    public void OnInspectorGUI(Transform tr)
    {
        EditorGUI.BeginChangeCheck();

        int oldSizeArray = segments.arraySize;
        serializedObject.Update();
        DisplaySplineOptions();


        if (oldSizeArray == 0 && segments.arraySize != oldSizeArray)
        {
            for (int i = 0; i < segments.arraySize; i++)
            {
                SerializedProperty item = segments.GetArrayElementAtIndex(i);
                SerializedProperty p1 = item.FindPropertyRelative("p1");
                SerializedProperty p2 = item.FindPropertyRelative("p2");
                p1.vector3Value = tr.position;
                p2.vector3Value = tr.position;
            }
        }

        serializedObject.ApplyModifiedProperties();
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
        if (newSize != segments.arraySize && newSize >= 0)
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

    public void OnSceneGUI(Transform trComp, ref Spline.Segment[] segmentsComp, ref Vector3 center)
    {
        EditorGUI.BeginChangeCheck();

        currentEvt = Event.current;

        // You'll need a control id to avoid messing with other tools!
        var controlID = GUIUtility.GetControlID(FocusType.Passive);
        var eventType = currentEvt.GetTypeForControl(controlID);

        if (segments.arraySize > 0)
            Tools.current = Tool.None;
        else return;


        if (useCatmullRom.boolValue)
        {
            DisplayCatmullRomSpline(trComp, ref segmentsComp);
        }

        if (!circleShape.boolValue)
        {
            SetFreeSegments(trComp, ref segmentsComp);
        }
        else
        {
            SetCircleSegments(trComp, ref segmentsComp, ref center, radius.floatValue);
        }

        if (currentEvt.control && currentEvt.button == 0)
        {
            if (!RemovePoint(trComp, ref segmentsComp))
            {
                if (currentEvt.type == EventType.MouseDown)
                    AddPoint(trComp, ref segmentsComp);
            }
        }
    }

    private void SetFreeSegments(Transform trComp, ref Spline.Segment[] segmentsComp)
    {
        Transform parent = trComp.parent;
        Vector3 firstP = Vector3.zero;
        Vector3 LastP = Vector3.zero;
        Vector3 newP1 = Vector3.zero;
        Vector3 newP2 = Vector3.zero;

        float dist = 0.0f;
        for (int i = 0; i < segmentsComp.Length; ++i)
        {

            if (i == 0)
            {
                newP1 = DrawHandle(trComp, -1, i.ToString(), segmentsComp[i].p1, Quaternion.identity);

                segmentsComp[i].p1 = newP1;
                firstP = newP1;
            }
            else
            {
                newP1 = LastP;
                if (i >= segmentsComp.Length) break;
                segmentsComp[i].p1 = newP1;
            }

            if (close.boolValue && i == segments.arraySize - 1)
            {
                newP2 = firstP;
            }
            else
            {
                newP2 = DrawHandle(trComp, i, (i + 1).ToString(), segmentsComp[i].p2, Quaternion.identity);
            }

            segmentsComp[i].p2 = newP2;

            segmentsComp[i].p1length = dist;

            segmentsComp[i].length = (newP2 - newP1).magnitude;

            dist += segmentsComp[i].length;

            segmentsComp[i].p2length = dist;

            segmentsComp[i].dir = (newP2 - newP1).normalized;

            if (parent)
            {
                Handles.DrawDottedLine(parent.TransformPoint(newP1), parent.TransformPoint(newP2), 5.0f);
            }
            else Handles.DrawDottedLine(newP1, newP2, 5.0f);

            LastP = newP2;
        }
    }

    private void SetCircleSegments(Transform trComp, ref Spline.Segment[] segmentsComp, ref Vector3 center, float _radius)
    {
        Transform parent = trComp.parent;
        Vector3 firstP = Vector3.zero;
        Vector3 LastP = Vector3.zero;
        Vector3 newP1 = Vector3.zero;
        Vector3 newP2 = Vector3.zero;
        Vector3 newCenter = Vector3.zero;

        float PI2 = Mathf.PI * ((demiCircle.boolValue) ? 1.0f : 2.0f);
        float DivPI2 = PI2 / ((close.boolValue) ? segments.arraySize : (segments.arraySize + 1));

        Quaternion eulerRot = Quaternion.Euler(rotation.vector3Value.x, rotation.vector3Value.y, rotation.vector3Value.z);

        newCenter = DrawHandle(trComp, -1, "center", center, Quaternion.identity);//Handles.PositionHandle(component.Center, Quaternion.identity);
        center = newCenter;

        float dist = 0.0f;

        for (int i = 0; i < segmentsComp.Length; ++i)
        {
            if (i == 0)
            {
                newP1 = DrawHandle(trComp, i, i.ToString(), newCenter + eulerRot * new Vector3(Mathf.Cos(DivPI2 * i), Mathf.Sin(DivPI2 * i)) * _radius, Quaternion.identity);

                segmentsComp[i].p1 = newP1;
                firstP = newP1;
            }
            else
            {
                newP1 = LastP;
                if (i >= segmentsComp.Length) break;
                segmentsComp[i].p1 = newP1;
            }

            if (close.boolValue && i == segments.arraySize - 1)
            {
                newP2 = firstP;
            }
            else
            {
                newP2 = DrawHandle(trComp, i, (i + 1).ToString(), newCenter + eulerRot * new Vector3(Mathf.Cos(DivPI2 * (i + 1)), Mathf.Sin(DivPI2 * (i + 1))) * _radius, Quaternion.identity);
            }

            segmentsComp[i].p1length = dist;

            segmentsComp[i].length = (newP2 - newP1).magnitude;

            segmentsComp[i].p2 = newP2;

            dist += segmentsComp[i].length;

            segmentsComp[i].p2length = dist;

            segmentsComp[i].dir = (newP2 - newP1).normalized;
            if (parent)
            {
                Handles.DrawDottedLine(parent.TransformPoint(newP1), parent.TransformPoint(newP2), 5.0f);
            }
            else
                Handles.DrawDottedLine(newP1, newP2, 5.0f);

            LastP = newP2;
        }
    }

    private Vector3 DrawHandle(Transform trComp, int id, string name, Vector3 position, Quaternion rotation)
    {
        Transform parent = trComp.parent;
        Vector3 newPos = position;
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 15;
        style.alignment = TextAnchor.MiddleCenter;

        if (currentEvt.modifiers != EventModifiers.Control &&
        Handles.Button((parent) ? parent.transform.TransformPoint(position) : position, Quaternion.identity, radiusHandle, radiusHandle, Handles.SphereHandleCap))
        {
            if ((circleShape.boolValue && id == -1) || !circleShape.boolValue)
            {
                SelectPoint(id);
            }
        }

        if (idPointSelects.Contains(id))
        {
            if (parent)
            {
                Vector3 npos = Handles.PositionHandle(parent.TransformPoint(position), Quaternion.identity);
                newPos = parent.InverseTransformPoint(npos);
            }
            else newPos = Handles.PositionHandle(position, Quaternion.identity);
        }
        else
        {
            if ((circleShape.boolValue && id == -1) || !circleShape.boolValue)
            {
                Handles.SphereHandleCap(0, (parent) ? parent.TransformPoint(position) : position, Quaternion.identity, radiusHandle, EventType.Repaint);
            }
        }

        Handles.Label((parent) ? parent.TransformPoint(position) : position + Vector3.up * radiusHandle * 2.0f, name, style);

        return newPos;
    }

    public virtual void Initialize()
    {
        currentDist = serializedObject.FindProperty("currentDist");

        circleShape = serializedObject.FindProperty("circleShape");
        center = serializedObject.FindProperty("center");
        demiCircle = serializedObject.FindProperty("demiCircle");
        radius = serializedObject.FindProperty("radius");
        rotation = serializedObject.FindProperty("rotation");

        segments = serializedObject.FindProperty("segments");

        useCatmullRom = serializedObject.FindProperty("useCatmullRom");

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

    private void DisplayCatmullRomSpline(Transform trComp, ref Spline.Segment[] segmentsComp)
    {
        //Draw the Catmull-Rom spline between the points
        for (int i = 0; i < segmentsComp.Length; ++i)
        {
            DisplayCatmullRomSpline(trComp, ref segmentsComp, i);
        }
    }

    private void DisplayCatmullRomSpline(Transform trComp, ref Spline.Segment[] segmentsComp, int pos)
    {
        Transform parent = trComp.parent;
        //The start position of the line
        Vector3 lastPos = segmentsComp[segmentsComp.ClampListPos(pos)].p1;

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
                newPos = SplineUtils.GetCatmullRomPosition(t,
                segmentsComp[segmentsComp.ClampListPos(pos - 1)].p1,
                segmentsComp[segmentsComp.ClampListPos(pos)].p1,
                segmentsComp[segmentsComp.ClampListPos(pos)].p2,
                segmentsComp[segmentsComp.ClampListPos(pos + 1)].p2);
            }
            else
            {
                if (pos == 0)
                {
                    newPos = SplineUtils.GetCatmullRomPosition(t,
                    segmentsComp[segmentsComp.ClampListPos(pos)].p1,
                    segmentsComp[segmentsComp.ClampListPos(pos)].p1,
                    segmentsComp[segmentsComp.ClampListPos(pos)].p2,
                    segmentsComp[segmentsComp.ClampListPos(pos + 1)].p2);
                }
                else if (pos == segments.arraySize - 1)
                {
                    newPos = SplineUtils.GetCatmullRomPosition(t,
                    segmentsComp[segmentsComp.ClampListPos(pos - 1)].p1,
                    segmentsComp[segmentsComp.ClampListPos(pos)].p1,
                    segmentsComp[segmentsComp.ClampListPos(pos)].p2,
                    segmentsComp[segmentsComp.ClampListPos(pos)].p2);
                }
                else
                {
                    newPos = SplineUtils.GetCatmullRomPosition(t,
                    segmentsComp[segmentsComp.ClampListPos(pos - 1)].p1,
                    segmentsComp[segmentsComp.ClampListPos(pos)].p1,
                    segmentsComp[segmentsComp.ClampListPos(pos)].p2,
                    segmentsComp[segmentsComp.ClampListPos(pos + 1)].p2);
                }
            }

            //Draw this line segment
            Handles.color = Color.blue;
            if (parent)
            {
                Handles.DrawLine(parent.TransformPoint(lastPos), parent.TransformPoint(newPos));
            }
            else Handles.DrawLine(lastPos, newPos);
            Handles.color = Color.white;
            //Save this pos so we can draw the next line segment
            lastPos = newPos;
        }
    }

    private void DisplaySplineOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Spline", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(useCatmullRom);


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

    private void AddPoint(Transform trComp, ref Spline.Segment[] segmentsComp)
    {
        Transform parent = trComp.parent;

        Ray r = HandleUtility.GUIPointToWorldRay(currentEvt.mousePosition);

        float dist = 0.0f;
        Vector3 position = Vector3.zero;

        if (parent)
        {
            dist = (parent.TransformPoint(segmentsComp[segmentsComp.Length - 1].p2) - r.origin).magnitude;
            position = parent.InverseTransformPoint(r.origin) + r.direction * dist;
        }
        else
        {
            dist = (segmentsComp[segmentsComp.Length - 1].p2 - r.origin).magnitude;
            position = r.origin + r.direction * dist;
        }

        int length = segmentsComp.Length;

        Spline.Segment[] tmpArray = new Spline.Segment[length];
        System.Array.Copy(segmentsComp, tmpArray, length);

        segmentsComp = new Spline.Segment[length + 1];

        for (int i = 0; i < segmentsComp.Length; ++i)
        {
            if (i == segmentsComp.Length - 1)
            {
                segmentsComp[i] = new Spline.Segment();
                if (close.boolValue)
                {
                    segmentsComp[i].p1 = position;
                    segmentsComp[i].p2 = segmentsComp[0].p1;

                    segmentsComp[i - 1].p2 = position;
                }
                else
                {
                    segmentsComp[i].p1 = segmentsComp[i - 1].p2;
                    segmentsComp[i].p2 = position;
                }
            }
            else segmentsComp[i] = new Spline.Segment(tmpArray[i]);
        }

        Repaint();
    }

    private bool RemovePoint(Transform trComp, ref Spline.Segment[] segmentsComp)
    {
        Transform parent = trComp.parent;

        idPointSelects.Clear();
        if (parent)
        {
            for (int i = 0; i < segmentsComp.Length; ++i)
            {
                if (Handles.Button(parent.TransformPoint(segmentsComp[i].p2), Quaternion.identity, radiusHandle, radiusHandle, Handles.SphereHandleCap)
                    || Handles.Button(parent.TransformPoint(segmentsComp[i].p1), Quaternion.identity, radiusHandle, radiusHandle, Handles.SphereHandleCap))
                {
                    RemovePoint(ref segmentsComp, i);
                    return true;
                }
            }
        }
        else
        {
            for (int i = 0; i < segmentsComp.Length; ++i)
            {
                if (Handles.Button(segmentsComp[i].p2, Quaternion.identity, radiusHandle, radiusHandle, Handles.SphereHandleCap)
                    || Handles.Button(segmentsComp[i].p1, Quaternion.identity, radiusHandle, radiusHandle, Handles.SphereHandleCap))
                {
                    RemovePoint(ref segmentsComp, i);
                    return true;
                }
            }
        }

        return false;
    }

    private void RemovePoint(ref Spline.Segment[] segmentsComp, int id)
    {
        int length = segmentsComp.Length;
        int indexTmp = 0;
        Spline.Segment[] tmpArray = new Spline.Segment[length];
        System.Array.Copy(segmentsComp, tmpArray, length);

        segmentsComp = new Spline.Segment[length - 1];

        for (int i = 0; i < segmentsComp.Length; ++i)
        {
            if (i == id)
            {
                indexTmp++;
                segmentsComp[i] = new Spline.Segment(tmpArray[indexTmp]);

                if ((i - 1) >= 0)
                {
                    segmentsComp[i - 1].p2 = segmentsComp[i].p1;
                }
            }
            else
            {
                segmentsComp[i] = new Spline.Segment(tmpArray[indexTmp]);
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
}
