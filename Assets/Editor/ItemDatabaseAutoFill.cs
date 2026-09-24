using UnityEngine;
using UnityEditor;

public static class ItemDatabaseAutoFill
{
    [MenuItem("RPG Scene Builder/Refresh Item Database", false, 40)]
    public static void RefreshDatabase()
    {
        // FindAnyObjectByType replaces the deprecated FindObjectOfType.
        // Editor-only scene search is fine here — no runtime facade needed.
        ItemDatabase database = Object.FindAnyObjectByType<ItemDatabase>();
        if (database == null)
        {
            Debug.LogWarning("No ItemDatabase found in the open scene. " +
                             "It lives on the Player prefab — is the Player in the scene?");
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