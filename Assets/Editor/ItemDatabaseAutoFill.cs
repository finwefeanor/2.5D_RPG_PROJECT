using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class ItemDatabaseAutoFill
{
    [MenuItem("RPG Scene Builder/Refresh Item Database")]
    public static void RefreshDatabase()
    {
        ItemDatabase database = Object.FindObjectOfType<ItemDatabase>();
        if (database == null)
        {
            Debug.LogWarning("No ItemDatabase found in the open scene.");
            return;
        }

        // Find every ItemData asset anywhere in the project
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        database.allItems.Clear();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null)
                database.allItems.Add(item);
        }

        EditorUtility.SetDirty(database); // marks the scene/object as changed so Unity saves it
        Debug.Log($"ItemDatabase refreshed: {database.allItems.Count} items found.");
    }
}
