#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Editor
{
    // Simple property drawer for individual WeaponChance elements
    [CustomPropertyDrawer(typeof(WeaponChance))]
    public class WeaponChanceDrawer : PropertyDrawer
    {
        // Layout configuration - adjust these values to control the layout
        private const float WeaponWidthRatio = 0.55f;  // Weapon field width (0.0 to 1.0)
        private const float SliderWidthRatio = 0.26f;     // Slider width (0.0 to 1.0) 
        private const float IntFieldWidth = 30f;         // Fixed width for the int field in pixels
        private const float LockButtonWidth = 20f;       // Width for the lock button
        private const float Spacing = 5f;                  // Spacing between elements
    
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Get the weapon and chance properties
            var weaponProperty = property.FindPropertyRelative("weapon");
            var chanceProperty = property.FindPropertyRelative("chance");
            var isLockedProperty = property.FindPropertyRelative("isLocked");

            // Calculate rects based on configuration
            var weaponRect = new Rect(position.x, position.y, position.width * WeaponWidthRatio, position.height);
            var sliderRect = new Rect(weaponRect.xMax + Spacing, position.y, position.width * SliderWidthRatio, position.height);
            var intFieldRect = new Rect(sliderRect.xMax + Spacing, position.y, IntFieldWidth, position.height);
            var lockButtonRect = new Rect(intFieldRect.xMax + Spacing, position.y, LockButtonWidth, position.height);

            // Draw weapon object field with preserved properties
            var weaponLabel = EditorGUI.BeginProperty(weaponRect, GUIContent.none, weaponProperty);
            var customLabel = new GUIContent("", weaponLabel.tooltip);
            EditorGUI.PropertyField(weaponRect, weaponProperty, customLabel, true);
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

    // Custom property drawer for weaponChances array to add equalize button
    [CustomPropertyDrawer(typeof(WeaponChance[]))]
    public class WeaponChanceArrayDrawer : PropertyDrawer
    {
        private ReorderableList reorderableList;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Check if this is being drawn for a Resource with SpecialWeapon type
            var resourceTypeProperty = property.serializedObject.FindProperty("resourceType");
            bool isSpecialWeapon = resourceTypeProperty != null && resourceTypeProperty.enumValueIndex == (int)ResourceType.SpecialWeapon;
            
            if (!isSpecialWeapon)
            {
                // If not SpecialWeapon, draw normally (VInspector will handle visibility)
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            
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
            // Check if this is being drawn for a Resource with SpecialWeapon type
            var resourceTypeProperty = property.serializedObject.FindProperty("resourceType");
            bool isSpecialWeapon = resourceTypeProperty != null && resourceTypeProperty.enumValueIndex == (int)ResourceType.SpecialWeapon;
            
            if (!isSpecialWeapon)
            {
                // If not SpecialWeapon, return default height
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
            
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
        
            // Calculate positions to align with the columns below
            var weaponHeaderRect = new Rect(rect.x, rect.y, rect.width * 0.55f, rect.height);
            var chanceHeaderRect = new Rect(rect.x + rect.width * 0.55f + 3f, rect.y, rect.width * 0.35f, rect.height);
            var lockHeaderRect = new Rect(rect.x + rect.width - 25f, rect.y, 25f, rect.height);
        
            GUI.Label(weaponHeaderRect, "Weapon", headerStyle);
            GUI.Label(chanceHeaderRect, "Chance %", chanceHeaderStyle);
            GUI.Label(lockHeaderRect, "🔒", chanceHeaderStyle);
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
            if (arrayProperty.arraySize == 0) return;
        
            // Count only unlocked entries with valid weapons
            var unlockedIndices = new System.Collections.Generic.List<int>();
            int lockedTotal = 0;
        
            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                var element = arrayProperty.GetArrayElementAtIndex(i);
                var weaponProp = element.FindPropertyRelative("weapon");
                var isLockedProp = element.FindPropertyRelative("isLocked");
                var chanceProp = element.FindPropertyRelative("chance");
            
                // Only consider entries with valid weapons
                if (weaponProp.objectReferenceValue != null)
                {
                    if (isLockedProp.boolValue)
                    {
                        lockedTotal += chanceProp.intValue;
                    }
                    else
                    {
                        unlockedIndices.Add(i);
                    }
                }
            }
        
            if (unlockedIndices.Count == 0) return; // All valid weapons are locked
        
            // Calculate remaining percentage for unlocked entries
            int remainingPercentage = Mathf.Max(0, 100 - lockedTotal);
            int equalChance = remainingPercentage / unlockedIndices.Count;
            int remainder = remainingPercentage % unlockedIndices.Count;
        
            // Set equal chances for unlocked entries only
            for (int i = 0; i < unlockedIndices.Count; i++)
            {
                var element = arrayProperty.GetArrayElementAtIndex(unlockedIndices[i]);
                var chanceProp = element.FindPropertyRelative("chance");
            
                // Give equal chance to unlocked entries, with remainder distributed to first entries
                chanceProp.intValue = equalChance + (i < remainder ? 1 : 0);
            }
        
            arrayProperty.serializedObject.ApplyModifiedProperties();
        
            var resource = arrayProperty.serializedObject.targetObject as Resource;
            if (resource != null)
            {
                EditorUtility.SetDirty(resource);
            }
        }
    }
}

#endif