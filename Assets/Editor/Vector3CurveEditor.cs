using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(Vector3Curve))]
public class Vector3CurveEditor : PropertyDrawer
{
    private float propertyHeight = 20.0f;
    private bool displayRangeX = false;
    private bool displayRangeY = false;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return propertyHeight * (5 - ((!displayRangeX) ? 1 : 0) - ((!displayRangeY) ? 1 : 0));
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty range = property.FindPropertyRelative("range");
        SerializedProperty curveX = property.FindPropertyRelative("curveX");
        SerializedProperty curveY = property.FindPropertyRelative("curveY");
        SerializedProperty curveZ = property.FindPropertyRelative("curveZ");

        MonoBehaviour mono = property.serializedObject.targetObject as MonoBehaviour;
        FieldInfo objectField = mono.GetType().GetField(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);//.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Curve3DAttribute attribute = null;

        int RangeX = 1;
        int RangeY = 1;

        if (objectField != null)
        {

            attribute = System.Attribute.GetCustomAttribute(objectField, typeof(Curve3DAttribute)) as Curve3DAttribute;
            if (attribute != null)
            {
                displayRangeX = attribute.RangeX;
                displayRangeY = attribute.RangeY;
            }
        }
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        EditorGUI.BeginProperty(position, label, property);

        position.height = propertyHeight;


        if (displayRangeX)
        {
            RangeX = EditorGUI.IntField(position, "Range X", range.vector2IntValue.x);
            position.y += propertyHeight + 2;
        }

        if (displayRangeY)
        {
            RangeY = EditorGUI.IntField(position, "Range Y", range.vector2IntValue.y);
            position.y += propertyHeight + 2;
        }

        range.vector2IntValue = new Vector2Int(RangeX, RangeY);

        Rect ranges = new Rect(0, 0, range.vector2IntValue.x, range.vector2IntValue.y);



        EditorGUI.CurveField(position, curveX, Color.red, ranges);
        position.y += propertyHeight;

        EditorGUI.CurveField(position, curveY, Color.green, ranges);
        position.y += propertyHeight;

        EditorGUI.CurveField(position, curveZ, Color.blue, ranges);
        position.y += propertyHeight;

        EditorGUI.EndProperty();

    }
}
