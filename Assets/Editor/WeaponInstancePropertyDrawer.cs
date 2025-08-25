using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Editor
{
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(WeaponInstance))]
public class WeaponInstancePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Early return if property is null
        if (property == null) return;
        
        // Get the weaponData property with null check (corrected field name)
        SerializedProperty weaponDataProp = property.FindPropertyRelative("weaponData");
        if (weaponDataProp == null) return;
        
        // Get the parent array to determine the index
        string propertyPath = property.propertyPath;
        int arrayIndex = GetArrayIndex(propertyPath);
        
        // Create custom label
        string customLabel = "Element " + arrayIndex;
        
        if (weaponDataProp.objectReferenceValue != null)
        {
            SOWeaponData weaponData = weaponDataProp.objectReferenceValue as SOWeaponData;
            if (weaponData != null && !string.IsNullOrEmpty(weaponData.WeaponName))
            {
                if (arrayIndex == 0)
                {
                    customLabel = weaponData.WeaponName + " (Starting Weapon)";
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
            
            // Draw weaponData field (corrected field name)
            SerializedProperty weaponData = property.FindPropertyRelative("weaponData");
            if (weaponData != null)
            {
                EditorGUI.PropertyField(
                    new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight),
                    weaponData
                );
                yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            
            // Draw weaponGfx field
            SerializedProperty weaponGfx = property.FindPropertyRelative("weaponGfx");
            if (weaponGfx != null)
            {
                EditorGUI.PropertyField(
                    new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight),
                    weaponGfx
                );
                yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            
            // Draw weaponReticle field (corrected field name)
            SerializedProperty weaponReticle = property.FindPropertyRelative("weaponReticle");
            if (weaponReticle != null)
            {
                EditorGUI.PropertyField(
                    new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight),
                    weaponReticle
                );
                yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            
            // Draw weaponBarrels array
            SerializedProperty weaponBarrels = property.FindPropertyRelative("weaponBarrels");
            if (weaponBarrels != null)
            {
                float barrelHeight = EditorGUI.GetPropertyHeight(weaponBarrels, true);
                EditorGUI.PropertyField(
                    new Rect(position.x, yPos, position.width, barrelHeight),
                    weaponBarrels,
                    true
                );
                yPos += barrelHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            
            // Check if we should show upgrade section
            if (ShouldShowUpgradeSection(weaponDataProp.objectReferenceValue as SOWeaponData))
            {
                // Auto-sync upgrade assets before drawing
                AutoSyncUpgradeAssets(property);
                
                // Draw upgrade assets array with custom label (no bold)
                SerializedProperty upgradeAssets = property.FindPropertyRelative("upgradeAssets");
                if (upgradeAssets != null)
                {
                    float upgradeAssetsHeight = EditorGUI.GetPropertyHeight(upgradeAssets, true);
                    EditorGUI.PropertyField(
                        new Rect(position.x, yPos, position.width, upgradeAssetsHeight),
                        upgradeAssets,
                        new GUIContent("Upgrades"),
                        true
                    );
                    yPos += upgradeAssetsHeight + EditorGUIUtility.standardVerticalSpacing;
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Early return with default height if property is null
        if (property == null)
            return EditorGUIUtility.singleLineHeight;
        
        float height = EditorGUIUtility.singleLineHeight;
        
        if (property.isExpanded)
        {
            // Add height for each base field with null checks (corrected field names)
            SerializedProperty weaponData = property.FindPropertyRelative("weaponData");
            if (weaponData != null)
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty weaponGfx = property.FindPropertyRelative("weaponGfx");
            if (weaponGfx != null)
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty weaponReticle = property.FindPropertyRelative("weaponReticle");
            if (weaponReticle != null)
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Add height for weaponBarrels array
            SerializedProperty weaponBarrels = property.FindPropertyRelative("weaponBarrels");
            if (weaponBarrels != null)
            {
                try
                {
                    height += EditorGUI.GetPropertyHeight(weaponBarrels, true) + EditorGUIUtility.standardVerticalSpacing;
                }
                catch (System.Exception)
                {
                    // Fallback to single line height if GetPropertyHeight fails
                    height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }
            }
            
            // Check if we need upgrade section (corrected field name)
            SerializedProperty weaponDataProp = property.FindPropertyRelative("weaponData");
            if (weaponDataProp != null && ShouldShowUpgradeSection(weaponDataProp.objectReferenceValue as SOWeaponData))
            {
                // Add height for upgrade assets array
                SerializedProperty upgradeAssets = property.FindPropertyRelative("upgradeAssets");
                if (upgradeAssets != null)
                {
                    try
                    {
                        height += EditorGUI.GetPropertyHeight(upgradeAssets, true) + EditorGUIUtility.standardVerticalSpacing;
                    }
                    catch (System.Exception)
                    {
                        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
            }
        }
        
        return height;
    }
    
    private bool ShouldShowUpgradeSection(SOWeaponData weaponData)
    {
        if (weaponData == null || weaponData.WeaponUpgrades == null || weaponData.WeaponUpgrades.Count == 0)
            return false;
        
        // Check if any upgrade has visual overrides
        try
        {
            foreach (var upgrade in weaponData.WeaponUpgrades)
            {
                if (upgrade != null && (upgrade.OverrideWeaponGfx || upgrade.OverrideWeaponBarrels))
                {
                    return true;
                }
            }
        }
        catch (System.Exception)
        {
            // If we can't access the upgrades, don't show the section
            return false;
        }
        
        return false;
    }
    
    private void AutoSyncUpgradeAssets(SerializedProperty property)
    {
        SerializedProperty weaponDataProp = property.FindPropertyRelative("weaponData");
        SerializedProperty upgradeAssetsProp = property.FindPropertyRelative("upgradeAssets");
        
        if (weaponDataProp?.objectReferenceValue == null || upgradeAssetsProp == null) return;
        
        SOWeaponData weaponData = weaponDataProp.objectReferenceValue as SOWeaponData;
        if (weaponData?.WeaponUpgrades == null) return;
        
        bool needsSync = false;
        
        // Check if array sizes match
        if (upgradeAssetsProp.arraySize != weaponData.WeaponUpgrades.Count)
        {
            needsSync = true;
        }
        else
        {
            // Check if upgrade references match
            for (int i = 0; i < weaponData.WeaponUpgrades.Count; i++)
            {
                SerializedProperty assetElement = upgradeAssetsProp.GetArrayElementAtIndex(i);
                SerializedProperty upgradeProp = assetElement.FindPropertyRelative("upgrade");
                
                if (upgradeProp?.objectReferenceValue != weaponData.WeaponUpgrades[i])
                {
                    needsSync = true;
                    break;
                }
            }
        }
        
        if (needsSync)
        {
            // Resize and sync the array
            upgradeAssetsProp.arraySize = weaponData.WeaponUpgrades.Count;
            
            for (int i = 0; i < weaponData.WeaponUpgrades.Count; i++)
            {
                SerializedProperty assetElement = upgradeAssetsProp.GetArrayElementAtIndex(i);
                SerializedProperty upgradeProp = assetElement.FindPropertyRelative("upgrade");
                
                if (upgradeProp != null)
                {
                    upgradeProp.objectReferenceValue = weaponData.WeaponUpgrades[i];
                }
            }
            
            upgradeAssetsProp.serializedObject.ApplyModifiedProperties();
        }
    }
    
    private int GetArrayIndex(string propertyPath)
    {
        if (string.IsNullOrEmpty(propertyPath))
            return 0;
        
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

[CustomPropertyDrawer(typeof(WeaponUpgradeAssets))]
public class WeaponUpgradeAssetsPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty upgradeProp = property.FindPropertyRelative("upgrade");
        
        // Early return if no upgrade or upgrade doesn't have visual overrides
        if (upgradeProp?.objectReferenceValue == null)
            return;
            
        SOWeaponUpgrade upgrade = upgradeProp.objectReferenceValue as SOWeaponUpgrade;
        if (upgrade == null || (!upgrade.OverrideWeaponGfx && !upgrade.OverrideWeaponBarrels))
            return;
        
        string displayName = upgrade.ItemName;
        
        EditorGUI.BeginProperty(position, label, property);
        
        // Create a GUIStyle for normal (non-bold) text
        GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
        foldoutStyle.fontStyle = FontStyle.Normal;
        
        // Show foldout with normal text style
        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded,
            displayName,
            true,
            foldoutStyle
        );
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Draw upgrade GFX
            if (upgrade.OverrideWeaponGfx)
            {
                SerializedProperty upgradeGfx = property.FindPropertyRelative("upgradeGfx");
                if (upgradeGfx != null)
                {
                    EditorGUI.PropertyField(
                        new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight),
                        upgradeGfx,
                        new GUIContent("GFX")
                    );
                    yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            if (upgrade.OverrideWeaponBarrels)
            {
                // Draw upgrade barrels
                SerializedProperty upgradeBarrels = property.FindPropertyRelative("upgradeBarrels");
                if (upgradeBarrels != null)
                {
                    float barrelsHeight = EditorGUI.GetPropertyHeight(upgradeBarrels, true);
                    EditorGUI.PropertyField(
                        new Rect(position.x, yPos, position.width, barrelsHeight),
                        upgradeBarrels,
                        new GUIContent("Barrels"),
                        true
                    );
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty upgradeProp = property.FindPropertyRelative("upgrade");
        
        // Return 0 height if no upgrade or upgrade doesn't have visual overrides
        if (upgradeProp?.objectReferenceValue == null)
            return 0;
            
        SOWeaponUpgrade upgrade = upgradeProp.objectReferenceValue as SOWeaponUpgrade;
        if (upgrade == null || (!upgrade.OverrideWeaponGfx && !upgrade.OverrideWeaponBarrels))
            return 0;
        
        float height = EditorGUIUtility.singleLineHeight;
        
        if (property.isExpanded)
        {
            // Add height for GFX field if override is enabled
            if (upgrade.OverrideWeaponGfx)
            {
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            
            // Add height for barrels array if override is enabled
            if (upgrade.OverrideWeaponBarrels)
            {
                SerializedProperty upgradeBarrels = property.FindPropertyRelative("upgradeBarrels");
                if (upgradeBarrels != null)
                {
                    height += EditorGUI.GetPropertyHeight(upgradeBarrels, true) + EditorGUIUtility.standardVerticalSpacing;
                }
            }
        }
        
        return height;
    }
}
#endif
}