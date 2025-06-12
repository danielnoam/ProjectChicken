#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Editor
{
    // Simple property drawer for individual ResourceChance elements
    [CustomPropertyDrawer(typeof(ResourceChance))]
    public class ResourceChanceDrawer : PropertyDrawer
    {
        // Layout configuration - adjust these values to control the layout
        private const float ResourceWidthRatio = 0.55f;  // Resource field width (0.0 to 1.0)
        private const float SliderWidthRatio = 0.26f;     // Slider width (0.0 to 1.0) 
        private const float IntFieldWidth = 30f;         // Fixed width for the int field in pixels
        private const float LockButtonWidth = 20f;       // Width for the lock button
        private const float Spacing = 5f;                  // Spacing between elements
    
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Get the resource and chance properties
            var resourceProperty = property.FindPropertyRelative("resource");
            var chanceProperty = property.FindPropertyRelative("chance");
            var isLockedProperty = property.FindPropertyRelative("isLocked");

            // Calculate rects based on configuration
            var resourceRect = new Rect(position.x, position.y, position.width * ResourceWidthRatio, position.height);
            var sliderRect = new Rect(resourceRect.xMax + Spacing, position.y, position.width * SliderWidthRatio, position.height);
            var intFieldRect = new Rect(sliderRect.xMax + Spacing, position.y, IntFieldWidth, position.height);
            var lockButtonRect = new Rect(intFieldRect.xMax + Spacing, position.y, LockButtonWidth, position.height);

            // Draw resource object field with preserved properties
            var resourceLabel = EditorGUI.BeginProperty(resourceRect, GUIContent.none, resourceProperty);
            var customLabel = new GUIContent("", resourceLabel.tooltip);
            EditorGUI.PropertyField(resourceRect, resourceProperty, customLabel, true);
            EditorGUI.EndProperty();

            // Draw chance slider (without a label) - disable if locked
            EditorGUI.BeginDisabledGroup(isLockedProperty.boolValue);
            EditorGUI.BeginChangeCheck();
            float newChance = GUI.HorizontalSlider(sliderRect, chanceProperty.intValue, 0f, 100f);
            if (EditorGUI.EndChangeCheck())
            {
                chanceProperty.intValue = Mathf.RoundToInt(newChance);
            }
        
            // Draw separate int field - disable if locked
            EditorGUI.BeginChangeCheck();
            int intValue = EditorGUI.IntField(intFieldRect, chanceProperty.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                chanceProperty.intValue = Mathf.Clamp(intValue, 0, 100);
            }
            EditorGUI.EndDisabledGroup();
        
            // Draw lock checkbox
            var lockTooltip = isLockedProperty.boolValue ? "Chance value is locked" : "Chance value is unlocked";
            
            EditorGUI.BeginChangeCheck();
            bool isLocked = EditorGUI.Toggle(lockButtonRect, new GUIContent("", lockTooltip), isLockedProperty.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                isLockedProperty.boolValue = isLocked;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }

    // Custom editor for SOLootTable to handle headers and buttons
    [CustomEditor(typeof(SOLootTable))]
    public class SOLootTableEditor : UnityEditor.Editor
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
                        if (Attribute.GetCustomAttribute(field, typeof(HeaderAttribute)) is HeaderAttribute headerAttribute)
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
        
            // Calculate positions to align with the columns below - updated for new layout
            var resourceHeaderRect = new Rect(rect.x, rect.y, rect.width * 0.55f, rect.height);
            var chanceHeaderRect = new Rect(rect.x + rect.width * 0.55f + 3f, rect.y, rect.width * 0.35f, rect.height);
            var lockHeaderRect = new Rect(rect.x + rect.width - 25f, rect.y, 25f, rect.height);
        
            GUI.Label(resourceHeaderRect, "Resource", headerStyle);
            GUI.Label(chanceHeaderRect, "Chance %", chanceHeaderStyle);
            GUI.Label(lockHeaderRect, "🔒", chanceHeaderStyle);
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
    
        private void EqualizeAllChances(SerializedProperty arrayProperty)
        {
            if (arrayProperty.arraySize == 0) return;
        
            // Count only unlocked entries
            var unlockedIndices = new System.Collections.Generic.List<int>();
            int lockedTotal = 0;
        
            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                var element = arrayProperty.GetArrayElementAtIndex(i);
                var isLockedProp = element.FindPropertyRelative("isLocked");
                var chanceProp = element.FindPropertyRelative("chance");
            
                if (isLockedProp.boolValue)
                {
                    lockedTotal += chanceProp.intValue;
                }
                else
                {
                    unlockedIndices.Add(i);
                }
            }
        
            if (unlockedIndices.Count == 0) return; // All are locked
        
            // Calculate the remaining percentage for unlocked entries
            int remainingPercentage = Mathf.Max(0, 100 - lockedTotal);
            int equalChance = remainingPercentage / unlockedIndices.Count;
            int remainder = remainingPercentage % unlockedIndices.Count;
        
            // Set equal chances for unlocked entries only
            for (int i = 0; i < unlockedIndices.Count; i++)
            {
                var element = arrayProperty.GetArrayElementAtIndex(unlockedIndices[i]);
                var chanceProp = element.FindPropertyRelative("chance");
            
                // Give equal chance to unlocked entries, with the remainder distributed to first entries
                chanceProp.intValue = equalChance + (i < remainder ? 1 : 0);
            }
        
            serializedObject.ApplyModifiedProperties();
        
            // Trigger normalization
            var lootTable = target as SOLootTable;
            if (lootTable)
            {
                EditorUtility.SetDirty(lootTable);
            }
        }
    }
}

#endif