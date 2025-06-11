#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

// Simple property drawer for individual WeaponChance elements
[CustomPropertyDrawer(typeof(WeaponChance))]
public class WeaponChanceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get the weaponData and chance properties
        var weaponProperty = property.FindPropertyRelative("weapon");
        var chanceProperty = property.FindPropertyRelative("chance");

        // Calculate rects for weaponData field and slider
        var weaponRect = new Rect(position.x, position.y, position.width * 0.6f - 5f, position.height);
        var sliderRect = new Rect(position.x + position.width * 0.6f, position.y, position.width * 0.4f, position.height);

        // Draw weaponData object field with preserved properties
        var weaponLabel = EditorGUI.BeginProperty(weaponRect, GUIContent.none, weaponProperty);
        var customLabel = new GUIContent("", weaponLabel.tooltip);
        EditorGUI.PropertyField(weaponRect, weaponProperty, customLabel, true);
        EditorGUI.EndProperty();

        // Draw chance slider
        var oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 20f;
        
        EditorGUI.BeginChangeCheck();
        float newChance = EditorGUI.Slider(sliderRect, chanceProperty.floatValue, 0f, 100f);
        if (EditorGUI.EndChangeCheck())
        {
            chanceProperty.floatValue = newChance;
            DelayedNormalization(property);
        }

        EditorGUIUtility.labelWidth = oldLabelWidth;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
    
    private void DelayedNormalization(SerializedProperty property)
    {
        EditorApplication.delayCall += () => {
            if (property.serializedObject.targetObject != null)
            {
                var resource = property.serializedObject.targetObject as Resource;
                if (resource != null)
                {
                    EditorUtility.SetDirty(resource);
                }
            }
        };
    }
}

// Custom property drawer for weaponChances array only
[CustomPropertyDrawer(typeof(WeaponChance[]))]
public class WeaponChanceArrayDrawer : PropertyDrawer
{
    private ReorderableList reorderableList;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Initialize reorderable list if needed
        if (reorderableList == null || reorderableList.serializedProperty.propertyPath != property.propertyPath)
        {
            reorderableList = new ReorderableList(property.serializedObject, property, true, true, true, true)
            {
                drawHeaderCallback = DrawWeaponHeader,
                drawElementCallback = DrawWeaponElement,
                elementHeight = EditorGUIUtility.singleLineHeight + 2f,
                headerHeight = EditorGUIUtility.singleLineHeight + 4f
            };
        }
        
        EditorGUI.BeginProperty(position, label, property);
        
        float currentY = position.y;
        
        // Draw the reorderable list
        var listRect = new Rect(position.x, currentY, position.width, GetListHeight());
        reorderableList.DoList(listRect);
        currentY += GetListHeight() + 5f;
        
        // Draw equalize button
        var buttonRect = new Rect(position.x, currentY, position.width, 30f);
        if (GUI.Button(buttonRect, new GUIContent("Equalize All Weapon Chances", "Set all valid weapons to equal chance percentages")))
        {
            EqualizeAllWeaponChances(property);
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (reorderableList == null)
        {
            // Rough estimate for initial calculation
            return EditorGUIUtility.singleLineHeight * (property.arraySize + 3) + 40f;
        }
        
        return GetListHeight() + 35f; // List height + button + spacing
    }
    
    private float GetListHeight()
    {
        if (reorderableList == null) return 100f;
        
        return reorderableList.GetHeight();
    }
    
    private void DrawWeaponHeader(Rect rect)
    {
        var headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = EditorStyles.label.normal.textColor }
        };
        
        var chanceHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = EditorStyles.label.normal.textColor }
        };
        
        var weaponHeaderRect = new Rect(rect.x, rect.y, rect.width * 0.6f - 5f, rect.height);
        var chanceHeaderRect = new Rect(rect.x + rect.width * 0.6f, rect.y, rect.width * 0.4f, rect.height);
        
        GUI.Label(weaponHeaderRect, "Weapon", headerStyle);
        GUI.Label(chanceHeaderRect, "Chance %", chanceHeaderStyle);
    }
    
    private void DrawWeaponElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        var element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
        
        rect.y += 2f;
        rect.height = EditorGUIUtility.singleLineHeight;
        
        EditorGUI.PropertyField(rect, element, GUIContent.none);
    }
    
    private void EqualizeAllWeaponChances(SerializedProperty arrayProperty)
    {
        int validCount = 0;
        
        for (int i = 0; i < arrayProperty.arraySize; i++)
        {
            var element = arrayProperty.GetArrayElementAtIndex(i);
            var weaponProp = element.FindPropertyRelative("weapon");
            if (weaponProp.objectReferenceValue != null)
            {
                validCount++;
            }
        }
        
        if (validCount == 0) return;
        
        float equalChance = 100f / validCount;
        
        for (int i = 0; i < arrayProperty.arraySize; i++)
        {
            var element = arrayProperty.GetArrayElementAtIndex(i);
            var weaponProp = element.FindPropertyRelative("weapon");
            var chanceProp = element.FindPropertyRelative("chance");
            
            if (weaponProp.objectReferenceValue != null)
            {
                chanceProp.floatValue = equalChance;
            }
            else
            {
                chanceProp.floatValue = 0f;
            }
        }
        
        arrayProperty.serializedObject.ApplyModifiedProperties();
        
        var resource = arrayProperty.serializedObject.targetObject as Resource;
        if (resource != null)
        {
            EditorUtility.SetDirty(resource);
        }
    }
}

#endif