using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(StageEvent), true)]
    public class StageEventDrawer : PropertyDrawer 
    {
        private static Dictionary<string, Type> _typeMap;
        private static readonly Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) 
        {
            if (_typeMap == null) BuildTypeMap();
        
            // Calculate rects
            var typeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var contentRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, position.height - EditorGUIUtility.singleLineHeight);
        
            EditorGUI.BeginProperty(position, label, property);
            
            var typeName = property.managedReferenceFullTypename;
            var displayName = GetShortTypeName(typeName);
            var propertyPath = property.propertyPath;

            // Get or create foldout state for this property
            if (!_foldoutStates.ContainsKey(propertyPath))
                _foldoutStates[propertyPath] = true;

            // Create a rect for the foldout arrow and type dropdown
            var foldoutRect = new Rect(typeRect.x, typeRect.y, 15, typeRect.height);
            var dropdownRect = new Rect(typeRect.x + 15, typeRect.y, typeRect.width - 15, typeRect.height);

            // Draw foldout arrow only if we have an event selected
            if (property.managedReferenceValue != null)
            {
                _foldoutStates[propertyPath] = EditorGUI.Foldout(foldoutRect, _foldoutStates[propertyPath], GUIContent.none);
            }

            // Draw the type selection dropdown
            var dropdownContent = new GUIContent(displayName ?? "Select Event Type");
            if (EditorGUI.DropdownButton(dropdownRect, dropdownContent, FocusType.Keyboard)) 
            {
                ShowEventTypeMenu(property, typeName);
            }

            // Draw the properties if foldout is expanded and we have an event
            if (property.managedReferenceValue != null && _foldoutStates[propertyPath]) 
            {
                EditorGUI.indentLevel++;
                
                // Draw event description at the top for easy identification
                var descriptionProperty = property.FindPropertyRelative("eventDescription");
                if (descriptionProperty != null)
                {
                    var descRect = new Rect(contentRect.x, contentRect.y, contentRect.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(descRect, descriptionProperty);
                    
                    // Adjust content rect for remaining properties
                    contentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    contentRect.height -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }
                
                // Draw all properties except eventDescription and isActive (runtime only)
                var iterator = property.Copy();
                var endProperty = iterator.GetEndProperty();
                iterator.NextVisible(true);
                
                while (iterator.NextVisible(false) && !SerializedProperty.EqualContents(iterator, endProperty))
                {
                    if (iterator.name != "eventDescription" && iterator.name != "isActive")
                    {
                        var propHeight = EditorGUI.GetPropertyHeight(iterator, true);
                        var propRect = new Rect(contentRect.x, contentRect.y, contentRect.width, propHeight);
                        EditorGUI.PropertyField(propRect, iterator, true);
                        contentRect.y += propHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
                
                EditorGUI.indentLevel--;
            }
        
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) 
        {
            float height = EditorGUIUtility.singleLineHeight; // Type selection dropdown
            
            if (property.managedReferenceValue != null)
            {
                var propertyPath = property.propertyPath;
                if (_foldoutStates.ContainsKey(propertyPath) && _foldoutStates[propertyPath])
                {
                    // Add height for event description
                    height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    
                    // Calculate height for all other properties
                    var iterator = property.Copy();
                    var endProperty = iterator.GetEndProperty();
                    iterator.NextVisible(true);
                    
                    while (iterator.NextVisible(false) && !SerializedProperty.EqualContents(iterator, endProperty))
                    {
                        if (iterator.name != "eventDescription" && iterator.name != "isActive")
                        {
                            height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
                        }
                    }
                }
            }
            
            return height;
        }

        private void ShowEventTypeMenu(SerializedProperty property, string currentTypeName)
        {
            var menu = new GenericMenu();
            
            // Add null option to clear the event
            menu.AddItem(new GUIContent("None"), string.IsNullOrEmpty(currentTypeName), () => {
                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            });
            
            menu.AddSeparator("");
            
            if (_typeMap == null || _typeMap.Count == 0) 
            {
                menu.AddDisabledItem(new GUIContent("No Event types available"));
            }
            else
            {
                foreach (var kvp in _typeMap.OrderBy(k => k.Key))
                {
                    var name = kvp.Key;
                    var type = kvp.Value;
                    
                    menu.AddItem(new GUIContent(name), type.FullName == currentTypeName, () => {
                        property.managedReferenceValue = Activator.CreateInstance(type);
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }
            }
            
            menu.ShowAsContext();
        }

        private static void BuildTypeMap() 
        {
            var baseType = typeof(StageEvent);
            _typeMap = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm => {
                    try { return asm.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t) && t != baseType)
                .ToDictionary(t => GetNiceEventName(t), t => t);
        }

        private static string GetShortTypeName(string fullTypeName) 
        {
            if (string.IsNullOrEmpty(fullTypeName)) return null;
            var parts = fullTypeName.Split(' ');
            var typeName = parts.Length > 1 ? parts[1].Split('.').Last() : fullTypeName;
            return GetNiceEventName(typeName);
        }
        
        private static string GetNiceEventName(Type type)
        {
            return GetNiceEventName(type.Name);
        }
        
        private static string GetNiceEventName(string typeName)
        {
            // Remove "Event" suffix if present
            if (typeName.EndsWith("Event"))
                typeName = typeName.Substring(0, typeName.Length - 5);
            
            return ObjectNames.NicifyVariableName(typeName);
        }
    }
}