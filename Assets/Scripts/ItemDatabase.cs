using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public List<ItemData> allItems = new List<ItemData>(); // drag all 4 ItemData assets here in Inspector

    public ItemData GetByName(string itemName)
    {
        foreach (var item in allItems)
            if (item.itemName == itemName)
                return item;

        Debug.LogWarning($"ItemDatabase: no item found named '{itemName}'");
        return null;
    }
}
