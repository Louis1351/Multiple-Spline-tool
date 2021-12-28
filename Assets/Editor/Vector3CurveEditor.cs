using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(Vector3Curve))]
public class Vector3CurveEditor : PropertyDrawer
{
    private float propertyHeight = 20.0f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return propertyHeight * 3;
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {

        SerializedProperty curveX = property.FindPropertyRelative("curveX");
        SerializedProperty curveY = property.FindPropertyRelative("curveY");
        SerializedProperty curveZ = property.FindPropertyRelative("curveZ");

        Rect ranges = new Rect(0, 0, 1, 1);

        EditorGUI.BeginProperty(position, label, property);

        position.height = propertyHeight;

        EditorGUI.CurveField(position, curveX, Color.red, ranges);
        position.y += propertyHeight;

        EditorGUI.CurveField(position, curveY, Color.green, ranges);
        position.y += propertyHeight;

        EditorGUI.CurveField(position, curveZ, Color.blue, ranges);
        position.y += propertyHeight;

        EditorGUI.EndProperty();

    }
}
