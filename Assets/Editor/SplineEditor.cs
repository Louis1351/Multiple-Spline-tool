using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SplineEditor : Editor
{
    protected SerializedProperty center;
    protected SerializedProperty demiCircle;
    protected SerializedProperty rotation;
    protected SerializedProperty currentDist;
    protected SerializedProperty radius;
    protected SerializedProperty segments;

    protected SerializedProperty useCatmullRom;
    protected SerializedProperty close;

    protected SerializedProperty circleShape;
    protected bool oldClose;
    protected Event currentEvt = null;

    protected SerializedProperty radiusHandle;
    protected SerializedProperty colorHandle;
    protected List<int> idPointSelects;

    protected Spline component;

    public virtual void OnEnable()
    {
        oldClose = close.boolValue;
    }

    public override void OnInspectorGUI()
    {
        int oldSizeArray = segments.arraySize;

        DisplaySplineOptions();

        SerializedProperty item;
        SerializedProperty p1;
        SerializedProperty p2;

        if (oldSizeArray == 0 && segments.arraySize != oldSizeArray)
        {
            for (int i = 0; i < segments.arraySize; i++)
            {
                item = segments.GetArrayElementAtIndex(i);
                p1 = item.FindPropertyRelative("p1");
                p2 = item.FindPropertyRelative("p2");

                p1.vector3Value = component.transform.position;
                p2.vector3Value = component.transform.position;
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
        if (newSize != segments.arraySize && newSize >= 0)
            segments.arraySize = newSize;

        if (!circleShape.boolValue)
        {
            SerializedProperty item;
            SerializedProperty p1;
            SerializedProperty p2;

            for (int i = 0; i < segments.arraySize; ++i)
            {
                if (close.boolValue && i == segments.arraySize - 1)
                    break;

                item = segments.GetArrayElementAtIndex(i);
                p1 = item.FindPropertyRelative("p1");
                p2 = item.FindPropertyRelative("p2");

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

    public virtual void OnSceneGUI()
    {
        currentEvt = Event.current;

        // You'll need a control id to avoid messing with other tools!
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        EditorGUI.BeginChangeCheck();

        HandleSceneDrag();


        if (segments.arraySize > 0)
            Tools.current = Tool.None;
        else return;


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
                {
                    AddPoint(); 
                    currentEvt.Use();
                }
            }
        }

        EditorGUI.EndChangeCheck();
    }

    private void SetFreeSegments()
    {
        Transform parent = component.transform.parent;
        Vector3 firstP = Vector3.zero;
        Vector3 LastP = Vector3.zero;
        Vector3 newP1 = Vector3.zero;
        Vector3 newP2 = Vector3.zero;

        float dist = 0.0f;
        for (int i = 0; i < component.segments.Length; ++i)
        {

            if (i == 0)
            {
                newP1 = DrawHandle(-1, i.ToString(), component.segments[i].p1, Quaternion.identity);

                component.segments[i].p1 = newP1;
                firstP = newP1;
            }
            else
            {
                newP1 = LastP;
                if (i >= component.segments.Length) break;
                component.segments[i].p1 = newP1;
            }

            if (close.boolValue && i == segments.arraySize - 1)
            {
                newP2 = firstP;
            }
            else
            {
                newP2 = DrawHandle(i, (i + 1).ToString(), component.segments[i].p2, Quaternion.identity);
            }

            component.segments[i].p2 = newP2;

            component.segments[i].p1length = dist;

            component.segments[i].length = (newP2 - newP1).magnitude;

            dist += component.segments[i].length;

            component.segments[i].p2length = dist;

            component.segments[i].dir = (newP2 - newP1).normalized;

            if (parent)
            {
                Handles.DrawDottedLine(parent.TransformPoint(newP1), parent.TransformPoint(newP2), 5.0f);
            }
            else Handles.DrawDottedLine(newP1, newP2, 5.0f);

            LastP = newP2;
        }
    }

    private void SetCircleSegments(float _radius)
    {
        Transform parent = component.transform.parent;
        Vector3 firstP = Vector3.zero;
        Vector3 LastP = Vector3.zero;
        Vector3 newP1 = Vector3.zero;
        Vector3 newP2 = Vector3.zero;
        Vector3 newCenter = Vector3.zero;

        float PI2 = Mathf.PI * ((demiCircle.boolValue) ? 1.0f : 2.0f);
        float DivPI2 = PI2 / ((close.boolValue) ? segments.arraySize : (segments.arraySize + 1));

        Quaternion eulerRot = Quaternion.Euler(rotation.vector3Value.x, rotation.vector3Value.y, rotation.vector3Value.z);

        newCenter = DrawHandle(-1, "center", component.center, Quaternion.identity);//Handles.PositionHandle(component.Center, Quaternion.identity);
        component.center = newCenter;

        float dist = 0.0f;

        for (int i = 0; i < component.segments.Length; ++i)
        {
            if (i == 0)
            {
                newP1 = DrawHandle(i, i.ToString(), newCenter + eulerRot * new Vector3(Mathf.Cos(DivPI2 * i), Mathf.Sin(DivPI2 * i)) * _radius, Quaternion.identity);

                component.segments[i].p1 = newP1;
                firstP = newP1;
            }
            else
            {
                newP1 = LastP;
                if (i >= component.segments.Length) break;
                component.segments[i].p1 = newP1;
            }

            if (close.boolValue && i == segments.arraySize - 1)
            {
                newP2 = firstP;
            }
            else
            {
                newP2 = DrawHandle(i, (i + 1).ToString(), newCenter + eulerRot * new Vector3(Mathf.Cos(DivPI2 * (i + 1)), Mathf.Sin(DivPI2 * (i + 1))) * _radius, Quaternion.identity);
            }

            component.segments[i].p1length = dist;

            component.segments[i].length = (newP2 - newP1).magnitude;

            component.segments[i].p2 = newP2;

            dist += component.segments[i].length;

            component.segments[i].p2length = dist;

            component.segments[i].dir = (newP2 - newP1).normalized;
            if (parent)
            {
                Handles.DrawDottedLine(parent.TransformPoint(newP1), parent.TransformPoint(newP2), 5.0f);
            }
            else
                Handles.DrawDottedLine(newP1, newP2, 5.0f);

            LastP = newP2;
        }
    }

    private Vector3 DrawHandle(int id, string name, Vector3 position, Quaternion rotation)
    {
        Transform parent = component.transform.parent;
        Vector3 newPos = position;
        GUIStyle style = new GUIStyle();
        style.normal.textColor = colorHandle.colorValue;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 15;
        style.alignment = TextAnchor.MiddleCenter;

        if (currentEvt.modifiers != EventModifiers.Control &&
        Handles.Button((parent) ? parent.transform.TransformPoint(position) : position, Quaternion.identity, radiusHandle.floatValue, radiusHandle.floatValue, Handles.SphereHandleCap))
        {
            if ((circleShape.boolValue && id == -1) || !circleShape.boolValue)
            {
                SelectPoint(id);
            }
            currentEvt.Use();
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
                Handles.color = colorHandle.colorValue;
                Handles.SphereHandleCap(0, (parent) ? parent.TransformPoint(position) : position, Quaternion.identity, radiusHandle.floatValue, EventType.Repaint);
            }
        }

        Handles.Label((parent) ? parent.TransformPoint(position) : position + Vector3.up * radiusHandle.floatValue * 2.0f, name, style);

        return newPos;
    }

    public virtual void Initialize()
    {
        component = target as Spline;

        currentDist = serializedObject.FindProperty("currentDist");

        circleShape = serializedObject.FindProperty("circleShape");
        center = serializedObject.FindProperty("center");
        demiCircle = serializedObject.FindProperty("demiCircle");
        radius = serializedObject.FindProperty("radius");
        rotation = serializedObject.FindProperty("rotation");

        segments = serializedObject.FindProperty("segments");

        useCatmullRom = serializedObject.FindProperty("useCatmullRom");

        close = serializedObject.FindProperty("close");

        radiusHandle = serializedObject.FindProperty("radiusHandle");
        colorHandle = serializedObject.FindProperty("colorHandle");

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
        for (int i = 0; i < component.segments.Length; ++i)
        {
            DisplayCatmullRomSpline(i);
        }
    }

    private void DisplayCatmullRomSpline(int pos)
    {
        Transform parent = component.transform.parent;
        //The start position of the line
        Vector3 lastPos = component.segments[component.segments.ClampListPos(pos)].p1;

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
                component.segments[component.segments.ClampListPos(pos - 1)].p1,
                component.segments[component.segments.ClampListPos(pos)].p1,
                component.segments[component.segments.ClampListPos(pos)].p2,
                component.segments[component.segments.ClampListPos(pos + 1)].p2);
            }
            else
            {
                if (pos == 0)
                {
                    newPos = SplineUtils.GetCatmullRomPosition(t,
                    component.segments[component.segments.ClampListPos(pos)].p1,
                    component.segments[component.segments.ClampListPos(pos)].p1,
                    component.segments[component.segments.ClampListPos(pos)].p2,
                    component.segments[component.segments.ClampListPos(pos + 1)].p2);
                }
                else if (pos == segments.arraySize - 1)
                {
                    newPos = SplineUtils.GetCatmullRomPosition(t,
                    component.segments[component.segments.ClampListPos(pos - 1)].p1,
                    component.segments[component.segments.ClampListPos(pos)].p1,
                    component.segments[component.segments.ClampListPos(pos)].p2,
                    component.segments[component.segments.ClampListPos(pos)].p2);
                }
                else
                {
                    newPos = SplineUtils.GetCatmullRomPosition(t,
                    component.segments[component.segments.ClampListPos(pos - 1)].p1,
                    component.segments[component.segments.ClampListPos(pos)].p1,
                    component.segments[component.segments.ClampListPos(pos)].p2,
                    component.segments[component.segments.ClampListPos(pos + 1)].p2);
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

        DisplayDebugOptions();

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

    private void DisplayDebugOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(radiusHandle);
        EditorGUILayout.PropertyField(colorHandle);
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void AddPoint()
    {
        Transform parent = component.transform.parent;

        Ray r = HandleUtility.GUIPointToWorldRay(currentEvt.mousePosition);

        float dist = 0.0f;
        Vector3 position = Vector3.zero;

        if (parent)
        {
            dist = (parent.TransformPoint(component.segments[component.segments.Length - 1].p2) - r.origin).magnitude;
            position = parent.InverseTransformPoint(r.origin) + r.direction * dist;
        }
        else
        {
            dist = (component.segments[component.segments.Length - 1].p2 - r.origin).magnitude;
            position = r.origin + r.direction * dist;
        }

        int length = component.segments.Length;

        Spline.Segment[] tmpArray = new Spline.Segment[length];
        System.Array.Copy(component.segments, tmpArray, length);

        component.segments = new Spline.Segment[length + 1];

        for (int i = 0; i < component.segments.Length; ++i)
        {
            if (i == component.segments.Length - 1)
            {
                component.segments[i] = new Spline.Segment();
                if (close.boolValue)
                {
                    component.segments[i].p1 = position;
                    component.segments[i].p2 = component.segments[0].p1;

                    component.segments[i - 1].p2 = position;
                }
                else
                {
                    component.segments[i].p1 = component.segments[i - 1].p2;
                    component.segments[i].p2 = position;
                }
            }
            else component.segments[i] = new Spline.Segment(tmpArray[i]);
        }

        SceneView.RepaintAll();
    }

    private bool RemovePoint()
    {
        Transform parent = component.transform.parent;

        idPointSelects.Clear();
        if (parent)
        {
            for (int i = 0; i < component.segments.Length; ++i)
            {
                if (Handles.Button(parent.TransformPoint(component.segments[i].p2), Quaternion.identity, radiusHandle.floatValue, radiusHandle.floatValue, Handles.SphereHandleCap)
                    || Handles.Button(parent.TransformPoint(component.segments[i].p1), Quaternion.identity, radiusHandle.floatValue, radiusHandle.floatValue, Handles.SphereHandleCap))
                {
                    RemovePoint(i);
                    return true;
                }
            }
        }
        else
        {
            for (int i = 0; i < component.segments.Length; ++i)
            {
                if (Handles.Button(component.segments[i].p2, Quaternion.identity, radiusHandle.floatValue, radiusHandle.floatValue, Handles.SphereHandleCap)
                    || Handles.Button(component.segments[i].p1, Quaternion.identity, radiusHandle.floatValue, radiusHandle.floatValue, Handles.SphereHandleCap))
                {
                    RemovePoint(i);
                    return true;
                }
            }
        }

        return false;
    }

    private void RemovePoint(int id)
    {
        int length = component.segments.Length;
        int indexTmp = 0;
        Spline.Segment[] tmpArray = new Spline.Segment[length];
        System.Array.Copy(component.segments, tmpArray, length);

        component.segments = new Spline.Segment[length - 1];

        for (int i = 0; i < component.segments.Length; ++i)
        {
            if (i == id)
            {
                indexTmp++;
                component.segments[i] = new Spline.Segment(tmpArray[indexTmp]);

                if ((i - 1) >= 0)
                {
                    component.segments[i - 1].p2 = component.segments[i].p1;
                }
            }
            else
            {
                component.segments[i] = new Spline.Segment(tmpArray[indexTmp]);
            }
            indexTmp++;
        }

        SceneView.RepaintAll();
    }

    private void SelectPoint(int id)
    {
        if (!currentEvt.shift)
        {
            idPointSelects.Clear();
            currentEvt.Use();
        }

        if (!idPointSelects.Contains(id))
        {
            idPointSelects.Add(id);
            SceneView.RepaintAll();
        }
    }

    private void DebugShowSphere(Vector2 mousePosition)
    {
        Transform parent = component.transform.parent;
        Vector3 rPosition = Vector3.zero;
        Vector3 newPos = Vector3.zero;
        float newDist = 10000000000.0f;

        rPosition = HandleUtility.GUIPointToWorldRay(currentEvt.mousePosition).GetPoint(10);

        for (int i = 0; i < component.segments.Length; ++i)
        {
            Vector3 middle = (component.segments[i].p1 + component.segments[i].p2) * 0.5f;
            Vector3 posTemp = Vector3.Project((rPosition - component.segments[i].p1), (component.segments[i].p2 - component.segments[i].p1)) + component.segments[i].p1;
            float dist = (posTemp - middle).sqrMagnitude;

            if (dist < newDist)
            {
                newPos = posTemp;
                newDist = dist;
            }
        }

        Handles.SphereHandleCap(0, newPos, Quaternion.identity, radiusHandle.floatValue, EventType.Repaint);
    }

    // private bool isDragging = false;
    private void HandleSceneDrag()
    {
        /* if (currentEvt.type == EventType.MouseDown && currentEvt.button == 0 && GUIUtility.hotControl != 0)
         {
             //Debug.Log("start");
             GUIUtility.hotControl = controlID;
             isDragging = true;
         }

         if (currentEvt.type == EventType.MouseUp && Event.current.button == 0 && GUIUtility.hotControl == controlID)
         {
             // Debug.Log("end");
             isDragging = false;
             currentEvt.Use();
         }

         if (currentEvt.type == EventType.MouseDrag && Event.current.button == 0 && GUIUtility.hotControl == controlID)
         {
             // Debug.Log("drag");
             isDragging = true;
             currentEvt.Use();
         }*/

        /* if (isDragging)
         {*/
        DebugShowSphere(currentEvt.mousePosition);
        //   }
    }

    private void CreateSceneDragObjects()
    {

    }

    private void CleanUp(bool deleteTempSceneObject)
    {

    }
}
