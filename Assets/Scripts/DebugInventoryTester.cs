using System.Collections;
using System.Collections.Generic;
// ============================================================
//  DebugInventoryTester.cs  —  TEMPORARY, delete once
//  a real inventory UI exists.
//  Attach to the Player GameObject alongside InventoryManager.
// ============================================================
using UnityEngine;

public class DebugInventoryTester : MonoBehaviour
{
    [Header("Drag the ItemData assets you want to test")]
    public ItemData hatItem;
    public ItemData swordItem;

    private InventoryManager _inventoryManager;

    void Awake()
    {
        _inventoryManager = GetComponent<InventoryManager>();
    }

    void Update()
    {
        // Hat: I = add/equip, U = remove/unequip
        if (Input.GetKeyDown(KeyCode.I))
            _inventoryManager.AddItem(hatItem);
        if (Input.GetKeyDown(KeyCode.U))
            _inventoryManager.RemoveItem(hatItem);

        // Sword: G = add/equip, H = remove/unequip
        if (Input.GetKeyDown(KeyCode.G))
            _inventoryManager.AddItem(swordItem);
        if (Input.GetKeyDown(KeyCode.H))
            _inventoryManager.RemoveItem(swordItem);
    }
}
