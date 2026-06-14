// ============================================================
//  CharacterVisualController.cs  —  Assets/Scripts/
//  Attach to the Player GameObject.
//
//  Listens to EquipmentManager.OnEquipmentChanged and
//  updates the 3D outfit child object accordingly.
//  This is the ONLY script that touches the visual —
//  no other script should enable/disable the outfit directly.
// ============================================================
using UnityEngine;

public class CharacterVisualController : MonoBehaviour
{
    [Header("Outfit Object")]
    [Tooltip("The child primitive that represents equipped clothing. " +
             "Disable it by default in the Inspector.")]
    public GameObject outfitObject;

    private EquipmentManager _equipmentManager;
    private Material _outfitMaterial;

    void Awake()
    {
        _equipmentManager = GetComponent<EquipmentManager>();
        if (_equipmentManager == null)
        {
            Debug.LogError("CharacterVisualController requires EquipmentManager on the same GameObject.");
            return;
        }

        // Cache a material instance so we don't modify the shared asset
        if (outfitObject != null)
        {
            var renderer = outfitObject.GetComponent<Renderer>();
            if (renderer != null)
                _outfitMaterial = renderer.material = new Material(renderer.sharedMaterial);
        }

        // Subscribe to equipment changes
        _equipmentManager.OnEquipmentChanged += RefreshVisuals;
    }

    void OnDestroy()
    {
        // Always unsubscribe to avoid memory leaks
        if (_equipmentManager != null)
            _equipmentManager.OnEquipmentChanged -= RefreshVisuals;
    }

    void RefreshVisuals()
    {
        if (outfitObject == null) return;

        // Check if any visual slot is filled (Head or Chest)
        var headItem  = _equipmentManager.GetEquipped(EquipSlot.Head);
        var chestItem = _equipmentManager.GetEquipped(EquipSlot.Chest);

        // Priority: show chest item color, fallback to head item color
        ItemData activeItem = chestItem ?? headItem;

        if (activeItem != null)
        {
            outfitObject.SetActive(true);
            if (_outfitMaterial != null)
                _outfitMaterial.color = activeItem.outfitColor;
        }
        else
        {
            // Nothing equipped — hide outfit
            outfitObject.SetActive(false);
        }
    }
}
