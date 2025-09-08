using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(StageTask), true)]
    public class StageTaskDrawer : PropertyDrawer 
    {
        private static Dictionary<string, Type> _typeMap;
        private static readonly Dictionary<string, bool> FoldoutStates = new Dictionary<string, bool>();

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
            FoldoutStates.TryAdd(propertyPath, true);

            // Create a rect for the foldout arrow and type dropdown
            var foldoutRect = new Rect(typeRect.x, typeRect.y, 15, typeRect.height);
            var dropdownRect = new Rect(typeRect.x + 15, typeRect.y, typeRect.width - 15, typeRect.height);

            // Draw foldout arrow only if we have a task selected
            if (property.managedReferenceValue != null)
            {
                FoldoutStates[propertyPath] = EditorGUI.Foldout(foldoutRect, FoldoutStates[propertyPath], GUIContent.none);
            }

            // Draw the type selection dropdown
            var dropdownContent = new GUIContent(displayName ?? "Select Task Type");
            if (EditorGUI.DropdownButton(dropdownRect, dropdownContent, FocusType.Keyboard)) 
            {
                ShowTaskTypeMenu(property, typeName);
            }

            // Draw the properties if foldout is expanded and we have a task
            if (property.managedReferenceValue != null && FoldoutStates[propertyPath]) 
            {
                EditorGUI.indentLevel++;
                
                // Draw task description at the top for easy identification
                var descriptionProperty = property.FindPropertyRelative("taskDescription");
                if (descriptionProperty != null)
                {
                    var descRect = new Rect(contentRect.x, contentRect.y, contentRect.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(descRect, descriptionProperty);
                    
                    // Adjust content rect for remaining properties
                    contentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    contentRect.height -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }
                
                // Draw all properties except taskDescription and isCompleted (runtime only)
                var iterator = property.Copy();
                var endProperty = iterator.GetEndProperty();
                iterator.NextVisible(true);
                
                while (iterator.NextVisible(false) && !SerializedProperty.EqualContents(iterator, endProperty))
                {
                    if (iterator.name != "taskDescription" && iterator.name != "isCompleted")
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
                if (FoldoutStates.ContainsKey(propertyPath) && FoldoutStates[propertyPath])
                {
                    // Add height for task description
                    height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    
                    // Calculate height for all other properties
                    var iterator = property.Copy();
                    var endProperty = iterator.GetEndProperty();
                    iterator.NextVisible(true);
                    
                    while (iterator.NextVisible(false) && !SerializedProperty.EqualContents(iterator, endProperty))
                    {
                        if (iterator.name != "taskDescription" && iterator.name != "isCompleted")
                        {
                            height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
                        }
                    }
                }
            }
            
            return height;
        }

        private void ShowTaskTypeMenu(SerializedProperty property, string currentTypeName)
        {
            var menu = new GenericMenu();
            
            // Add null option to clear the task
            menu.AddItem(new GUIContent("None"), string.IsNullOrEmpty(currentTypeName), () => {
                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            });
            
            menu.AddSeparator("");
            
            if (_typeMap == null || _typeMap.Count == 0) 
            {
                menu.AddDisabledItem(new GUIContent("No Task types available"));
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
            var baseType = typeof(StageTask);
            _typeMap = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm => {
                    try { return asm.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t) && t != baseType)
                .ToDictionary(GetNiceTaskName, t => t);
        }

        private static string GetShortTypeName(string fullTypeName) 
        {
            if (string.IsNullOrEmpty(fullTypeName)) return null;
            var parts = fullTypeName.Split(' ');
            var typeName = parts.Length > 1 ? parts[1].Split('.').Last() : fullTypeName;
            return GetNiceTaskName(typeName);
        }
        
        private static string GetNiceTaskName(Type type)
        {
            return GetNiceTaskName(type.Name);
        }
        
        private static string GetNiceTaskName(string typeName)
        {
            // Remove "Task" suffix if present
            if (typeName.EndsWith("Task"))
                typeName = typeName.Substring(0, typeName.Length - 4);
            
            return ObjectNames.NicifyVariableName(typeName);
        }
    }
}