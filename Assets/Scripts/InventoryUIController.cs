// ============================================================
//  InventoryUIController.cs  —  Assets/Scripts/
//  Attach to the Player GameObject (alongside InventoryManager
//  and EquipmentManager) OR to the InventoryPanel itself —
//  either works, just needs references wired in the Inspector.
//
//  Generates one row per owned item at runtime, same pattern
//  as ShopManager.GenerateItemButtons(). Each row's button
//  either Equips or Unequips the item depending on whether
//  it's currently equipped in its slot.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIController : MonoBehaviour
{
    [Header("References")]
    public InventoryManager inventoryManager;
    public EquipmentManager equipmentManager;

    [Header("UI — assign in Inspector")]
    public GameObject panel;               // the whole InventoryPanel, toggled on/off
    public Transform rowContainer;        // content parent inside the ScrollView
    public GameObject rowPrefab;           // InventoryRowPrefab (duplicated from ItemButtonPrefab)

    private List<GameObject> _spawnedRows = new List<GameObject>();

        void Start()
    {
        var playerRefs = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (playerRefs == null)
        {
            Debug.LogError("InventoryUIController: no Player registered.", this);
            return;
        }
        inventoryManager = playerRefs.Inventory;
        equipmentManager = playerRefs.Equipment;

        var ui = ShopUIRefs.Instance;
        if (ui == null)
        {
            Debug.LogError("InventoryUIController: no ShopCanvas in scene.", this);
            return;
        }
        panel        = ui.inventoryPanel;
        rowContainer = ui.inventoryRowContainer;
        rowPrefab    = ui.inventoryRowPrefab;

        if (equipmentManager != null)
            equipmentManager.OnEquipmentChanged += RefreshInventoryUI;

            if (ui.inventoryCloseButton != null)
        {
            ui.inventoryCloseButton.onClick.RemoveAllListeners();
            ui.inventoryCloseButton.onClick.AddListener(TogglePanel);
            Debug.Log("Close button wired");
        }

        if (panel != null) panel.SetActive(false);

        RefreshInventoryUI();
    }

    void OnEnable()  => GameEvents.OnInventoryChanged += RefreshInventoryUI;
    void OnDisable() => GameEvents.OnInventoryChanged -= RefreshInventoryUI;

    void OnDestroy()
    {
        if (equipmentManager != null)
            equipmentManager.OnEquipmentChanged -= RefreshInventoryUI;
    }

    void Update()
    {
        // Toggle panel open/close — swap key if it collides with something else
        if (Input.GetKeyDown(KeyCode.Tab))
            TogglePanel();
    }

    public void TogglePanel()
    {
        if (panel == null) return;
        bool nowOpen = !panel.activeSelf;
        panel.SetActive(nowOpen);
        if (nowOpen) RefreshInventoryUI(); // catch any changes made while closed
    }

    // ── Row generation ────────────────────────────────────────

    public void RefreshInventoryUI()
    {
        if (rowContainer == null || rowPrefab == null || inventoryManager == null)
            return;

        foreach (var row in _spawnedRows)
            if (row != null) Destroy(row);
        _spawnedRows.Clear();

        foreach (var item in inventoryManager.OwnedItems)
        {
            var rowGO = Instantiate(rowPrefab, rowContainer);
            rowGO.SetActive(true);

            var labels = rowGO.GetComponentsInChildren<Text>();
            if (labels.Length >= 1) labels[0].text = item.itemName;

            bool isEquipped = equipmentManager != null &&
                               equipmentManager.GetEquipped(item.slot) == item;

            if (labels.Length >= 2)
                labels[1].text = isEquipped ? "Equipped" : "";

            var swatch = rowGO.transform.Find("Swatch");
            if (swatch != null)
            {
                var img = swatch.GetComponent<Image>();
                if (img != null) img.color = item.outfitColor;
            }

            var button = rowGO.GetComponent<Button>();
            if (button != null)
            {
                var capturedItem = item;
                var capturedEquipped = isEquipped;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnRowClicked(capturedItem, capturedEquipped));
            }

            _spawnedRows.Add(rowGO);
        }
    }

    void OnRowClicked(ItemData item, bool wasEquipped)
    {
        if (wasEquipped)
            equipmentManager.Unequip(item.slot);
        else
            inventoryManager.EquipOwnedItem(item);

        RefreshInventoryUI();
    }
}