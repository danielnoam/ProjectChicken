#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

// Simple property drawer for individual ResourceChance elements
[CustomPropertyDrawer(typeof(ResourceChance))]
public class ResourceChanceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get the resource and chance properties
        var resourceProperty = property.FindPropertyRelative("resource");
        var chanceProperty = property.FindPropertyRelative("chance");

        // Calculate rects for resource field and slider
        var resourceRect = new Rect(position.x, position.y, position.width * 0.6f - 5f, position.height);
        var sliderRect = new Rect(position.x + position.width * 0.6f, position.y, position.width * 0.4f, position.height);

        // Draw resource object field with preserved properties
        var resourceLabel = EditorGUI.BeginProperty(resourceRect, GUIContent.none, resourceProperty);
        var customLabel = new GUIContent("", resourceLabel.tooltip);
        EditorGUI.PropertyField(resourceRect, resourceProperty, customLabel, true);
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
                var lootTable = property.serializedObject.targetObject as SOLootTable;
                if (lootTable != null)
                {
                    EditorUtility.SetDirty(lootTable);
                }
            }
        };
    }
}

// Custom editor for SOLootTable to handle headers and buttons
[CustomEditor(typeof(SOLootTable))]
public class SOLootTableEditor : Editor
{
    private ReorderableList reorderableList;
    
    private void OnEnable()
    {
        var resourceChancesProperty = serializedObject.FindProperty("resourceChances");
        
        reorderableList = new ReorderableList(serializedObject, resourceChancesProperty, true, true, true, true)
        {
            drawHeaderCallback = DrawHeader,
            drawElementCallback = DrawElement,
            elementHeight = EditorGUIUtility.singleLineHeight + 2f,
            headerHeight = EditorGUIUtility.singleLineHeight + 4f // Standard header height
        };
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Get all properties
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            
            // Skip the script reference
            if (iterator.propertyPath == "m_Script")
                continue;
                
            // Handle resourceChances array specially
            if (iterator.propertyPath == "resourceChances")
            {
                // First draw the Header attribute if it exists
                var field = target.GetType().GetField("resourceChances", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                    
                if (field != null)
                {
                    var headerAttribute = System.Attribute.GetCustomAttribute(field, typeof(HeaderAttribute)) as HeaderAttribute;
                    if (headerAttribute != null)
                    {
                        EditorGUILayout.Space(8f);
                        var headerStyle = new GUIStyle(EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(headerAttribute.header, headerStyle);
                        EditorGUILayout.Space(2f);
                    }
                }
                
                reorderableList.DoLayoutList();
                
                EditorGUILayout.Space(5f);
                
                // Draw equalize button
                if (GUILayout.Button("Equalize All Chances", GUILayout.Height(30f)))
                {
                    EqualizeAllChances(reorderableList.serializedProperty);
                }
                
                EditorGUILayout.Space(5f);
            }
            else
            {
                // Draw all other properties normally
                EditorGUILayout.PropertyField(iterator, true);
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawHeader(Rect rect)
    {
        // Draw column headers using the default header space
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
        
        // Calculate positions to align with the columns below
        var resourceHeaderRect = new Rect(rect.x, rect.y, rect.width * 0.6f - 5f, rect.height);
        var chanceHeaderRect = new Rect(rect.x + rect.width * 0.6f, rect.y, rect.width * 0.4f, rect.height);
        
        GUI.Label(resourceHeaderRect, "Resource", headerStyle);
        GUI.Label(chanceHeaderRect, "Chance %", chanceHeaderStyle);
    }
    
    private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        var element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
        
        // Adjust rect to account for reorderable list margins
        rect.y += 2f;
        rect.height = EditorGUIUtility.singleLineHeight;
        
        // Draw the property using our custom drawer
        EditorGUI.PropertyField(rect, element, GUIContent.none);
    }
    
    private void DrawColumnHeaders(Rect rect)
    {
        // Draw background to match the list style
        EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f)); // Dark gray background
        
        var headerStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.white }
        };
        
        var chanceHeaderStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        
        // Calculate positions similar to the property drawer
        var resourceHeaderRect = new Rect(rect.x + 2f, rect.y, rect.width * 0.6f - 5f, rect.height);
        var chanceHeaderRect = new Rect(rect.x + rect.width * 0.6f, rect.y, rect.width * 0.4f, rect.height);
        
        GUI.Label(resourceHeaderRect, "Resource", headerStyle);
        GUI.Label(chanceHeaderRect, "Chance %", chanceHeaderStyle);
    }
    
    private void EqualizeAllChances(SerializedProperty arrayProperty)
    {
        int validCount = 0;
        
        // Count valid resources
        for (int i = 0; i < arrayProperty.arraySize; i++)
        {
            var element = arrayProperty.GetArrayElementAtIndex(i);
            var resourceProp = element.FindPropertyRelative("resource");
            if (resourceProp.objectReferenceValue != null)
            {
                validCount++;
            }
        }
        
        if (validCount == 0) return;
        
        float equalChance = 100f / validCount;
        
        // Set equal chances
        for (int i = 0; i < arrayProperty.arraySize; i++)
        {
            var element = arrayProperty.GetArrayElementAtIndex(i);
            var resourceProp = element.FindPropertyRelative("resource");
            var chanceProp = element.FindPropertyRelative("chance");
            
            if (resourceProp.objectReferenceValue != null)
            {
                chanceProp.floatValue = equalChance;
            }
            else
            {
                chanceProp.floatValue = 0f;
            }
        }
        
        serializedObject.ApplyModifiedProperties();
        
        // Trigger normalization
        var lootTable = target as SOLootTable;
        if (lootTable != null)
        {
            EditorUtility.SetDirty(lootTable);
        }
    }
}

#endif