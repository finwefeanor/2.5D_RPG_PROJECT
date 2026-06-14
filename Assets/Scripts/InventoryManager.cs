// ============================================================
//  InventoryManager.cs  —  Assets/Scripts/
//  Attach to the Player GameObject.
//
//  Owns the list of ItemData the player has bought.
//  Separating "owned items" from "equipped items" means
//  a player can own a Hat but have it unequipped — same
//  as every real RPG inventory system.
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    // All items the player currently owns
    private List<ItemData> _ownedItems = new List<ItemData>();

    private EquipmentManager _equipmentManager;

    void Awake()
    {
        _equipmentManager = GetComponent<EquipmentManager>();
        if (_equipmentManager == null)
            Debug.LogError("InventoryManager requires EquipmentManager on the same GameObject.");
    }

    // ── Public API ────────────────────────────────────────────

    // Called by ShopManager when player buys an item
    public void AddItem(ItemData item)
    {
        if (item == null) return;

        if (_ownedItems.Contains(item))
        {
            Debug.Log($"Already own: {item.itemName}");
            return;
        }

        _ownedItems.Add(item);
        Debug.Log($"Added to inventory: {item.itemName}");

        // Auto-equip on purchase if the slot is empty
        if (item.slot != EquipSlot.None && !_equipmentManager.IsSlotFilled(item.slot))
            _equipmentManager.Equip(item);
    }

    public bool OwnsItem(ItemData item)
    {
        return _ownedItems.Contains(item);
    }

    public List<ItemData> GetAllItems()
    {
        return _ownedItems;
    }
}
