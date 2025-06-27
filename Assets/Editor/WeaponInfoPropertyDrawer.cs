
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;



namespace Editor
{
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(WeaponInfo))]
public class WeaponInfoPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Get the weaponData property
        SerializedProperty weaponDataProp = property.FindPropertyRelative("weaponData");
        
        // Get the parent array to determine the index
        string propertyPath = property.propertyPath;
        int arrayIndex = GetArrayIndex(propertyPath);
        
        // Create custom label
        string customLabel = "Element " + arrayIndex;
        
        if (weaponDataProp.objectReferenceValue)
        {
            SOWeapon weaponData = weaponDataProp.objectReferenceValue as SOWeapon;
            if (weaponData && !string.IsNullOrEmpty(weaponData.WeaponName))
            {
                if (arrayIndex == 0)
                {
                    customLabel = weaponData.WeaponName + " (Base Weapon)";
                }
                else
                {
                    customLabel = weaponData.WeaponName + " (Special Weapon)";
                }
            }
        }
        
        // Begin property
        EditorGUI.BeginProperty(position, label, property);
        
        // Show foldout
        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded,
            customLabel,
            true
        );
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Draw weaponData field
            SerializedProperty weaponData = property.FindPropertyRelative("weaponData");
            EditorGUI.PropertyField(
                new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight),
                weaponData
            );
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Draw weaponGfx field
            SerializedProperty weaponGfx = property.FindPropertyRelative("weaponGfx");
            EditorGUI.PropertyField(
                new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight),
                weaponGfx
            );
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Draw weaponReticle field
            SerializedProperty weaponReticle = property.FindPropertyRelative("weaponReticle");
            EditorGUI.PropertyField(
                new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight),
                weaponReticle
            );
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Draw weaponBarrels array
            SerializedProperty weaponBarrels = property.FindPropertyRelative("weaponBarrels");
            EditorGUI.PropertyField(
                new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(weaponBarrels)),
                weaponBarrels,
                true
            );
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        
        if (property.isExpanded)
        {
            // Add height for each field
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // weaponData
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // weaponGfx
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // weaponReticle
            
            // Add height for weaponBarrels array
            SerializedProperty weaponBarrels = property.FindPropertyRelative("weaponBarrels");
            height += EditorGUI.GetPropertyHeight(weaponBarrels) + EditorGUIUtility.standardVerticalSpacing;
        }
        
        return height;
    }
    
    private int GetArrayIndex(string propertyPath)
    {
        // Extract array index from property path like "weapons.Array.data[0]"
        int startIndex = propertyPath.LastIndexOf('[');
        int endIndex = propertyPath.LastIndexOf(']');
        
        if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
        {
            string indexString = propertyPath.Substring(startIndex + 1, endIndex - startIndex - 1);
            if (int.TryParse(indexString, out int index))
            {
                return index;
            }
        }
        
        return 0;
    }
}
#endif
}