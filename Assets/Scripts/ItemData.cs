// ============================================================
//  ItemData.cs  —  Assets/Scripts/
//
//  ScriptableObject that defines a single item.
//  Create item assets via:
//  Right-click in Project window → Create → RPG → Item
//
//  One asset per item:
//    Hat.asset
//    Shirt.asset
//    (future) IronSword.asset, LeatherArmor.asset etc.
// ============================================================
using UnityEngine;
// Defines which slot an item occupies
// Add more slots here as your game grows (Weapon, Shield, Boots etc.)
public enum EquipSlot
{
    None,
    Head,
    Chest,
    RightHand,
    LeftHand
}

[CreateAssetMenu(fileName = "NewItem", menuName = "RPG/Item")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName      = "New Item";
    public Sprite icon;                          // used in shop UI button
    [TextArea(2, 4)]
    public string description   = "";

    [Header("Economy")]
    public int price            = 10;

    [Header("Equipment")]
    public EquipSlot slot       = EquipSlot.None; // None = not equippable
    public int defenseBonus     = 0;              // added to armor when equipped
    public int damageBonus      = 0;              // reserved for weapons later

    [Tooltip("Attack this item grants when in the RightHand slot. Null = falls back to unarmed.")]
    public AbilityDefinition attack;

    [Tooltip("Classes that may use this item. EMPTY = every class (weapons, shields, generic armour).")]
    public CharacterClassDefinition[] allowedClasses;

    // True if the given class may use this item. A player with no class assigned is never blocked.
    public bool CanBeUsedBy(CharacterClassDefinition playerClass)
    {
        if (allowedClasses == null || allowedClasses.Length == 0) return true;
        if (playerClass == null) return true;
        return System.Array.IndexOf(allowedClasses, playerClass) >= 0;
    }

    // "Barbarian" / "Mage, Rogue" - for shop labels.
    public string AllowedClassNames()
    {
        if (allowedClasses == null || allowedClasses.Length == 0) return "Any";
        var names = new System.Collections.Generic.List<string>();
        foreach (var c in allowedClasses)
            if (c != null) names.Add(c.className);
        return string.Join(", ", names);
    }

    [Header("Hand-slot visuals (Weapon/Shield only)")]
    [Tooltip("KayKit weapon/shield prefab to spawn at the hand socket. Leave null for Head/Chest items.")]
    public GameObject equipPrefab;
    public Vector3 equipPositionOffset;
    public Vector3 equipRotationOffset;

    [Header("3D Visual")]
    public Color outfitColor    = Color.white;    // color applied to outfit object
}
