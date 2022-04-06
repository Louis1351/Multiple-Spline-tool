using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SplineEditor : Editor
{
    protected enum SettingMode
    {
        none,
        Edit,
        Add,
        Remove
    }

    protected SerializedProperty semiCircle;
    protected SerializedProperty rotation;
    protected SerializedProperty currentDist;
    protected SerializedProperty radius;
    protected SerializedProperty segments;

    protected SerializedProperty useCatmullRom;
    protected SerializedProperty close;
    protected SerializedProperty snapToGrid;

    protected SerializedProperty circleShape;
    protected bool oldClose;
    protected Event currentEvt = null;

    protected SerializedProperty radiusHandle;
    protected SerializedProperty selectionColorHandle;
    protected SerializedProperty colorHandle;
    protected SerializedProperty showIndex;
    protected List<int> idPointSelects;

    private Spline component;
    protected SettingMode editSettingMode = SettingMode.none;
    private Texture[] textures;

    public Spline Component { get => component; set => component = value; }

    public virtual void OnEnable()
    {
        oldClose = close.boolValue;
    }
    private void OnDisable()
    {
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

    public virtual void OnSceneGUI()
    {
        currentEvt = Event.current;

        // You'll need a control id to avoid messing with other tools!
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        //EditorGUI.BeginChangeCheck();

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

        switch (editSettingMode)
        {
            case SettingMode.none:
                MovePivot();
                break;
            case SettingMode.Edit:
                break;
            case SettingMode.Remove:
                RemovePoint();
                break;
            case SettingMode.Add:
                AddNewPoint();
                break;
        }
        //  EditorGUI.EndChangeCheck();
    }

    private void DisplayButtonsSettingMode()
    {
        SettingMode mode = SettingMode.none;

        Rect rect = new Rect(10, 10, 100, 40);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        for (int i = 0; i < 3; ++i)
        {

            mode++;

            GUI.backgroundColor = (editSettingMode == mode) ? Color.green : Color.white;
            if (GUILayout.Button(textures[i], GUILayout.Width(40.0f), GUILayout.Height(40.0f)))
            {
                DeselectPoints();
                if (editSettingMode != mode)
                {
                    editSettingMode = mode;
                }
                else
                {
                    editSettingMode = SettingMode.none;
                }
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private void MovePivot()
    {
        Vector3 lastPosition = component.transform.position;
        Vector3 newPos = component.transform.position;

        EditorGUI.BeginChangeCheck();
        newPos = Handles.DoPositionHandle(component.transform.position, Quaternion.identity);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(component.transform, "Move Pivot");
            Undo.RecordObject(target, "Move Pivot");

            Vector3 displacement = newPos - lastPosition;

            component.segments[0].p1 += displacement;
            for (int i = 0; i < component.segments.Length; ++i)
            {
                component.segments[i].p2 += displacement;
            }
            component.transform.position = newPos;
        }
    }

    private void SetFreeSegments()
    {
        Transform parent = component.transform.parent;
        Vector3 LastP = Vector3.zero;
        Vector3 newP1 = Vector3.zero;
        Vector3 newP2 = Vector3.zero;
        Vector3 normal = Vector3.zero;

        float dist = 0.0f;
        for (int i = 0; i < component.segments.Length; ++i)
        {

            if (i == 0)
            {
                newP1 = DrawHandle(-1, i.ToString(), component.segments[i].p1, Quaternion.identity);

                component.segments[i].p1 = newP1;
            }
            else
            {
                newP1 = LastP;

                if (i >= component.segments.Length)
                    break;

                component.segments[i].p1 = newP1;

                if (i - 1 >= 0)
                    component.segments[i].angleP1 = component.segments[i - 1].angleP2;
            }

            if (close.boolValue && i == segments.arraySize - 1)
            {
                newP2 = component.segments[0].p1;
                component.segments[i].angleP2 = component.segments[0].angleP1;
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
        Vector3 LastP = Vector3.zero;
        Vector3 newP1 = Vector3.zero;
        Vector3 newP2 = Vector3.zero;

        float PI2 = Mathf.PI * ((semiCircle.boolValue) ? 1.0f : 2.0f);
        float DivPI2 = PI2 / ((close.boolValue) ? segments.arraySize : (segments.arraySize + 1));

        Quaternion eulerRot = Quaternion.Euler(rotation.vector3Value.x, rotation.vector3Value.y, rotation.vector3Value.z);

        float dist = 0.0f;

        for (int i = 0; i < component.segments.Length; ++i)
        {
            if (i == 0)
            {
                newP1 = DrawHandle(i, i.ToString(), component.transform.position + eulerRot * new Vector3(Mathf.Cos(DivPI2 * i), Mathf.Sin(DivPI2 * i)) * _radius, Quaternion.identity);

                component.segments[i].p1 = newP1;
            }
            else
            {
                newP1 = LastP;
                if (i >= component.segments.Length) break;
                component.segments[i].p1 = newP1;
            }

            if (close.boolValue && i == segments.arraySize - 1)
            {
                newP2 = component.segments[0].p1;
            }
            else
            {
                newP2 = DrawHandle(i, (i + 1).ToString(), component.transform.position + eulerRot * new Vector3(Mathf.Cos(DivPI2 * (i + 1)), Mathf.Sin(DivPI2 * (i + 1))) * _radius, Quaternion.identity);
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
        Color color = colorHandle.colorValue;
        Transform parent = component.transform.parent;
        Vector3 newPos = position;
        GUIStyle style = new GUIStyle();
        style.normal.textColor = color;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 15;
        style.alignment = TextAnchor.MiddleCenter;

        if (idPointSelects.Contains(id))
        {
            EditorGUI.BeginChangeCheck();
            if (parent)
            {
                Vector3 npos = Handles.PositionHandle(parent.TransformPoint(position), Quaternion.identity);
                newPos = parent.InverseTransformPoint(npos);
            }
            else newPos = Handles.PositionHandle(position, Quaternion.identity);

            if (newPos != position)
            {
                if (snapToGrid.boolValue)
                    newPos = SnapToGrid(newPos);

                Vector3 addOffset = newPos - position;

                for (int i = 0; i < idPointSelects.Count; ++i)
                {
                    if (idPointSelects[i] != id)
                    {
                        if (idPointSelects[i] == -1)
                            component.segments[0].p1 += addOffset;
                        else
                            component.segments[idPointSelects[i]].p2 += addOffset;
                    }

                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(component, "Move point " + id);
            }
        }
        else
        {
            Handles.color = color;
            if (Handles.Button((parent) ? parent.transform.TransformPoint(position) : position, Quaternion.identity, radiusHandle.floatValue, radiusHandle.floatValue, Handles.SphereHandleCap))
            {
                if (editSettingMode == SettingMode.Edit)
                {
                    if (currentEvt.modifiers != EventModifiers.Control)
                    {
                        if ((circleShape.boolValue && id == -1) || !circleShape.boolValue)
                        {
                            SelectPoint(id);
                        }
                        currentEvt.Use();
                    }

                    Handles.color = colorHandle.colorValue;
                }
            }
        }

        if (showIndex.boolValue)
            Handles.Label((parent) ? parent.TransformPoint(position) : position + Vector3.up * radiusHandle.floatValue * 2.0f, name, style);

        return newPos;
    }

    public virtual void Initialize()
    {
        component = target as Spline;

        currentDist = serializedObject.FindProperty("currentDist");

        circleShape = serializedObject.FindProperty("circleShape");
        semiCircle = serializedObject.FindProperty("semiCircle");
        radius = serializedObject.FindProperty("radius");
        rotation = serializedObject.FindProperty("rotation");

        segments = serializedObject.FindProperty("segments");

        useCatmullRom = serializedObject.FindProperty("useCatmullRom");

        close = serializedObject.FindProperty("close");
        snapToGrid = serializedObject.FindProperty("snapToGrid");

        radiusHandle = serializedObject.FindProperty("radiusHandle");
        colorHandle = serializedObject.FindProperty("colorHandle");
        selectionColorHandle = serializedObject.FindProperty("selectionColorHandle");
        showIndex = serializedObject.FindProperty("showIndex");

        textures = Resources.LoadAll<Texture>("SplineTextures");

        if (idPointSelects == null)
        {
            idPointSelects = new List<int>();
        }
        else
        {
            idPointSelects.Clear();
        }

        if (component.segments == null)
        {
            component.segments = new Spline.Segment[1];
            component.segments[0] = new Spline.Segment();
            component.segments[0].p1 = component.transform.position;
            component.segments[0].p2 = component.transform.position + Vector3.right;
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
      //  Vector3 normal = Vector3.zero;
      //  float angle = 0.0f;
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

         /*   if (component.segments[pos].angleP1 < component.segments[pos].angleP2)
                angle = Mathf.Lerp(component.segments[pos].angleP1, component.segments[pos].angleP2, t);
            else
                angle = Mathf.Lerp(component.segments[pos].angleP2, component.segments[pos].angleP1, t);*/

            /*Vector3 dir = (newPos - lastPos);

            normal = transform.TransformDirection(Vector3(0, -1, 0)); ;*/

          //  Handles.DrawLine(newPos, newPos + normal * 0.5f);

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

        DisplayEditionOptions();

        if (circleShape.boolValue)
            DisplayCircleOptions();

        DisplayDebugOptions();

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DisplayEditionOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        DisplayButtonsSettingMode();

        EditorGUILayout.LabelField("Edition", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(snapToGrid);


        /*  EditorGUI.BeginChangeCheck();
          int newSize = EditorGUILayout.IntField("Number of points", segments.arraySize);
          newSize = Mathf.Max(newSize, 1);

          if (EditorGUI.EndChangeCheck())
          {
              Undo.RecordObject(component, "Changed Number of points" + newSize);

              if (newSize >= 0)
                  segments.arraySize = newSize;
          }*/




        if (!circleShape.boolValue)
        {
            if (idPointSelects != null && idPointSelects.Count > 0)
            {
                EditorGUILayout.LabelField("Selected Points", EditorStyles.boldLabel);
            }

            SerializedProperty item;
            SerializedProperty p1;
            SerializedProperty p2;
            SerializedProperty angleP1;
            SerializedProperty angleP2;

            for (int i = 0; i < segments.arraySize; ++i)
            {
                if (close.boolValue && i == segments.arraySize - 1)
                    break;

                item = segments.GetArrayElementAtIndex(i);
                p1 = item.FindPropertyRelative("p1");
                p2 = item.FindPropertyRelative("p2");
                angleP1 = item.FindPropertyRelative("angleP1");
                angleP2 = item.FindPropertyRelative("angleP2");

                if (idPointSelects.Contains(-1) && i == 0)
                {
                    GUILayout.BeginVertical("GroupBox");
                    EditorGUILayout.LabelField("Point " + i, EditorStyles.boldLabel);
                    p1.vector3Value = EditorGUILayout.Vector3Field("position", p1.vector3Value);
                    angleP1.floatValue = EditorGUILayout.FloatField("rotation ", angleP1.floatValue);
                    GUILayout.EndVertical();
                }
                if (idPointSelects.Contains(i))
                {
                    GUILayout.BeginVertical("GroupBox");
                    EditorGUILayout.LabelField("Point " + (i + 1), EditorStyles.boldLabel);
                    p2.vector3Value = EditorGUILayout.Vector3Field("position", p2.vector3Value);
                    angleP2.floatValue = EditorGUILayout.FloatField("rotation ", angleP2.floatValue);
                    GUILayout.EndVertical();
                }
            }
        }

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DisplayCircleOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Circle", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(radius);

        EditorGUILayout.PropertyField(semiCircle);
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
        EditorGUILayout.PropertyField(selectionColorHandle);
        EditorGUILayout.PropertyField(showIndex);
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void AddPoint(Vector3 position, int segmentID)
    {
        Undo.RecordObject(component, "Add new Point ");

        int length = component.segments.Length;

        Spline.Segment[] tmpArray = new Spline.Segment[length];
        System.Array.Copy(component.segments, tmpArray, length);

        component.segments = new Spline.Segment[length + 1];
        int idTmp = 0;
        for (int i = 0; (i + idTmp) < component.segments.Length; ++i)
        {
            if (i == segmentID)
            {
                component.segments[i] = new Spline.Segment();
                component.segments[i].p1 = tmpArray[i].p1;
                component.segments[i].p2 = position;

                component.segments[i + 1] = new Spline.Segment();
                component.segments[i + 1].p1 = position;
                component.segments[i + 1].p2 = tmpArray[i].p2;

                idTmp++;
            }
            else component.segments[i + idTmp] = new Spline.Segment(tmpArray[i]);
        }

        segments.arraySize = (length + 1);

        SceneView.lastActiveSceneView.LookAt(position);
        SceneView.RepaintAll();
    }

    private void AddPoint(Vector3 position)
    {
        Undo.RecordObject(component, "Add new Point ");

        System.Array.Resize(ref component.segments, component.segments.Length + 1);

        if (close.boolValue)
        {
            component.segments[component.segments.Length - 2].p2 = position;

            component.segments[component.segments.Length - 1] = new Spline.Segment();
            component.segments[component.segments.Length - 1].p1 = position;
            component.segments[component.segments.Length - 1].p2 = component.segments[0].p1;
        }
        else
        {
            component.segments[component.segments.Length - 1] = new Spline.Segment();
            component.segments[component.segments.Length - 1].p1 = component.segments[component.segments.Length - 2].p2;
            component.segments[component.segments.Length - 1].p2 = position;
        }

        segments.arraySize = component.segments.Length;

        SceneView.lastActiveSceneView.LookAt(position);
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
        if (id == 0) return;


        int length = component.segments.Length;
        int indexTmp = 0;

        Undo.RecordObject(component, "Remove Point " + id);

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
            currentEvt.Use();
            DeselectPoints();
        }

        if (!idPointSelects.Contains(id))
        {
            idPointSelects.Add(id);

            if ((id + 1) >= component.segments.Length)
            {
                SceneView.lastActiveSceneView.LookAt(component.segments[component.segments.Length - 1].p2);
            }
            else SceneView.lastActiveSceneView.LookAt(component.segments[id + 1].p1);

            Repaint();
            SceneView.RepaintAll();
        }
    }

    public void DeselectPoints()
    {
        idPointSelects.Clear();
        Repaint();
    }

    private bool FindNewPointOnSegment(out Vector3 newPosition, out int segmentID, Vector2 mousePosition)
    {
        Transform parent = component.transform.parent;

        Plane plane;
        Ray ray = HandleUtility.GUIPointToWorldRay(currentEvt.mousePosition);

        bool findSomething = false;

        segmentID = 0;
        newPosition = Vector3.zero;

        float dTest = 0.0f;
        float maxDistance = 0.35f;

        Vector3 np = Vector3.zero;
        Camera currentCam = SceneView.lastActiveSceneView.camera;
        Vector3 middle = Vector3.zero;

        for (int i = 0; i < component.segments.Length; ++i)
        {
            if (parent)
            {
                middle = parent.TransformPoint((component.segments[i].p1 + component.segments[i].p2) * 0.5f);
                plane = new Plane(parent.TransformPoint(component.segments[i].p1), parent.TransformPoint(component.segments[i].p2), parent.TransformPoint(component.segments[i].p2) + currentCam.transform.up);
            }
            else
            {
                middle = (component.segments[i].p1 + component.segments[i].p2) * 0.5f;
                plane = new Plane(component.segments[i].p1, component.segments[i].p2, component.segments[i].p2 + currentCam.transform.up);
            }

            if (plane.Raycast(ray, out float enter))
            {
                newPosition = ray.GetPoint(enter);

                dTest = ((middle - newPosition).magnitude) / component.segments[i].length;

                if (dTest < maxDistance)
                {
                    maxDistance = dTest;

                    np = newPosition;
                    findSomething = true;

                    segmentID = i;
                }
            }
        }

        if (findSomething)
        {
            float d = component.segments[segmentID].p1length + (((parent) ? parent.TransformPoint(component.segments[segmentID].p1) : component.segments[segmentID].p1) - np).magnitude;
            newPosition = component.GetPositionAtDistance(d, false);

            Handles.color = selectionColorHandle.colorValue;
            Handles.SphereHandleCap(0, (parent) ? parent.TransformPoint(newPosition) : newPosition, Quaternion.identity, radiusHandle.floatValue, EventType.Repaint);
        }

        return findSomething;
    }

    private void AddNewPoint()
    {
        int segmentID = 0;
        if (FindNewPointOnSegment(out Vector3 pos, out segmentID, currentEvt.mousePosition))
        {
            if (currentEvt.type == EventType.MouseDown && currentEvt.button == 0 && GUIUtility.hotControl != 0)
            {
                GUIUtility.hotControl = 0;
                AddPoint(pos, segmentID);
            }
            SceneView.RepaintAll();
        }
        else
        {
            if (component.segments.Length > 0)
            {
                Transform parent = component.transform.parent;
                Camera currentCam = SceneView.lastActiveSceneView.camera;

                Ray ray = HandleUtility.GUIPointToWorldRay(currentEvt.mousePosition);
                Vector3 newPosition = ray.GetPoint((currentCam.transform.position - component.segments[component.segments.Length - 1].p2).magnitude);

                if (snapToGrid.boolValue)
                    newPosition = SnapToGrid(newPosition);

                Vector3 visualPosition = (parent) ? parent.TransformPoint(newPosition) : newPosition;

                Handles.color = selectionColorHandle.colorValue;

                if (close.boolValue)
                {
                    Handles.DrawDottedLine(visualPosition, component.segments[0].p1, 5.0f);
                    Handles.DrawDottedLine(component.segments[component.segments.Length - 1].p1, visualPosition, 5.0f);
                }
                else
                {
                    Handles.DrawDottedLine(component.segments[component.segments.Length - 1].p2, visualPosition, 5.0f);
                }

                Handles.SphereHandleCap(0, visualPosition, Quaternion.identity, radiusHandle.floatValue, EventType.Repaint);
                Handles.color = colorHandle.colorValue;

                if (currentEvt.type == EventType.MouseDown && currentEvt.button == 0 && GUIUtility.hotControl != 0)
                {
                    GUIUtility.hotControl = 0;
                    AddPoint(newPosition);
                }

                SceneView.RepaintAll();
            }
        }
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(Mathf.Round(position.x),
                             Mathf.Round(position.y),
                             Mathf.Round(position.z));
    }
}
