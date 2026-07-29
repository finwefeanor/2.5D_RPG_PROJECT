using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class SaveMenu
{
    [MenuItem("RPG Scene Builder/Clear Save Data")]
    public static void ClearSave()
    {
        SaveSystem.ClearSave();
        Debug.Log("Save data cleared via menu.");
    }
}
