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

    [Header("Hand-slot visuals (Weapon/Shield only)")]
    [Tooltip("KayKit weapon/shield prefab to spawn at the hand socket. Leave null for Head/Chest items.")]
    public GameObject equipPrefab;
    public Vector3 equipPositionOffset;
    public Vector3 equipRotationOffset;

    [Header("3D Visual")]
    public Color outfitColor    = Color.white;    // color applied to outfit object
}
