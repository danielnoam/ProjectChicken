#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(WeaponChance))]
public class WeaponChanceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get the weapon and chance properties
        var weaponProperty = property.FindPropertyRelative("weapon");
        var chanceProperty = property.FindPropertyRelative("chance");

        // Calculate rects for weapon field and slider
        var weaponRect = new Rect(position.x, position.y, position.width * 0.6f - 5f, position.height);
        var sliderRect = new Rect(position.x + position.width * 0.6f, position.y, position.width * 0.4f, position.height);

        // Draw weapon object field
        EditorGUI.PropertyField(weaponRect, weaponProperty, GUIContent.none);

        // Draw chance slider
        var oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 0;
        
        float newChance = EditorGUI.Slider(sliderRect, chanceProperty.floatValue, 0f, 100f);
        if (!Mathf.Approximately(newChance, chanceProperty.floatValue))
        {
            chanceProperty.floatValue = newChance;
        }

        EditorGUIUtility.labelWidth = oldLabelWidth;

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}

#endif