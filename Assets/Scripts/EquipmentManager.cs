// ============================================================
//  EquipmentManager.cs  —  Assets/Scripts/
//  Attach to the Player GameObject.
//
//  Tracks what ItemData is equipped in each EquipSlot.
//  Other scripts (PlayerHealth, NPCInteraction) ask this
//  for equipped item info instead of checking booleans.
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    // Slot → currently equipped item (null if nothing equipped)
    private Dictionary<EquipSlot, ItemData> _equipped
        = new Dictionary<EquipSlot, ItemData>();

    // Event fired whenever equipment changes
    // CharacterVisualController listens to this
    public event System.Action OnEquipmentChanged;

    // ── Public API ────────────────────────────────────────────

    public void Equip(ItemData item)
    {
        if (item == null || item.slot == EquipSlot.None)
        {
            Debug.LogWarning("EquipmentManager: Tried to equip null or non-equippable item.");
            return;
        }

        _equipped[item.slot] = item;
        Debug.Log($"Equipped: {item.itemName} in slot {item.slot}");
        OnEquipmentChanged?.Invoke();
    }

    public void Unequip(EquipSlot slot)
    {
        if (_equipped.ContainsKey(slot))
        {
            Debug.Log($"Unequipped slot: {slot}");
            _equipped.Remove(slot);
            OnEquipmentChanged?.Invoke();
        }
    }

    // Returns the item in a slot, or null if empty
    public ItemData GetEquipped(EquipSlot slot)
    {
        _equipped.TryGetValue(slot, out ItemData item);
        return item;
    }

    // True if any equippable item is currently equipped
    public bool HasAnythingEquipped()
    {
        return _equipped.Count > 0;
    }

    // True if a specific slot has something in it
    public bool IsSlotFilled(EquipSlot slot)
    {
        return _equipped.ContainsKey(slot) && _equipped[slot] != null;
    }

    // Total defense from all equipped items combined
    public int GetTotalDefense()
    {
        int total = 0;
        foreach (var item in _equipped.Values)
            total += item.defenseBonus;
        return total;
    }

    // Total damage bonus from all equipped items combined (for weapons later)
    public int GetTotalDamageBonus()
    {
        int total = 0;
        foreach (var item in _equipped.Values)
            total += item.damageBonus;
        return total;
    }
}
