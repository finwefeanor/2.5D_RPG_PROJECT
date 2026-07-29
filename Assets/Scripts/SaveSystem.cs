using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SaveSystem
{
    private const string GoldKey = "SaveGold";
    private const string OwnedItemsKey = "SaveOwnedItems";
    private const string EquippedPrefix = "SaveEquipped_"; // + slot name

    public static void Save(InventoryManager inventory, EquipmentManager equipment)
    {
        PlayerPrefs.SetInt(GoldKey, inventory.gold);

        // Owned items — comma-separated names
        string ownedNames = string.Join(",", inventory.GetAllItems().Select(i => i.itemName));
        PlayerPrefs.SetString(OwnedItemsKey, ownedNames);

        // Equipped items — one key per slot
        foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
        {
            if (slot == EquipSlot.None) continue;
            ItemData equipped = equipment.GetEquipped(slot);
            PlayerPrefs.SetString(EquippedPrefix + slot, equipped != null ? equipped.itemName : "");
        }

        PlayerPrefs.Save();
        Debug.Log($"SAVE: gold={inventory.gold}, items=[{ownedNames}]"); // <-- see exact values written
    }

    public static void Load(InventoryManager inventory, EquipmentManager equipment, ItemDatabase database)
    {
        if (!PlayerPrefs.HasKey(GoldKey))
        {
            Debug.Log("No save data found — using defaults.");
            return;
        }

        inventory.gold = PlayerPrefs.GetInt(GoldKey);
        string ownedNames = PlayerPrefs.GetString(OwnedItemsKey, "");
        Debug.Log($"LOAD: gold={inventory.gold}, raw saved items string=[{ownedNames}]"); // <-- see what's actually stored

        if (!string.IsNullOrEmpty(ownedNames))
        {
            foreach (string name in ownedNames.Split(','))
            {
                ItemData item = database.GetByName(name);
                Debug.Log($"LOAD: resolving '{name}' -> {(item != null ? item.itemName : "NULL")}");
                if (item != null)
                    inventory.AddItem(item);
            }
        }

        // Re-apply correct equips (AddItem's auto-equip may not match saved state exactly)
        foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
        {
            if (slot == EquipSlot.None) continue;
            string savedName = PlayerPrefs.GetString(EquippedPrefix + slot, "");
            if (!string.IsNullOrEmpty(savedName))
            {
                ItemData item = database.GetByName(savedName);
                if (item != null) equipment.Equip(item);
            }
            else
            {
                equipment.Unequip(slot);
            }
        }

        Debug.Log("Game loaded.");
    }

    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(GoldKey);
        PlayerPrefs.DeleteKey(OwnedItemsKey);
        foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
            PlayerPrefs.DeleteKey(EquippedPrefix + slot);
    }
}
