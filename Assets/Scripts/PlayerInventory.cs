using UnityEngine;
using UnityEngine.UI;

// BIGGEST CHANGE FROM 2D:
//   The entire SpriteRenderer + Sprite + Animator layer system is gone.
//   In 3D, "equipping clothes" is shown by:
//     1. Activating a child GameObject called "Outfit" (a differently-shaped/coloured primitive)
//     2. Changing its material color to match the purchased item
//   This gives you the same visual feedback with zero sprite work.
//
//   HOW TO SET UP IN EDITOR:
//     - On the Player GameObject, add a child called "Outfit"
//       (e.g. a slightly larger Capsule, or a flat Cube for a shirt silhouette)
//     - Assign it to the outfitObject field below
//     - The equip/unequip buttons work the same as before

public class PlayerInventory : MonoBehaviour
{
    [Header("3D Outfit Object")]
    public GameObject outfitObject;     // child primitive that represents equipped clothing
                                        // disable it by default in the Inspector

    [Header("UI Buttons")]
    public Button equipButton;
    public Button unequipButton;

    // Tracks the last purchased item color so Equip button can re-apply it
    private Color lastEquippedColor = Color.white;
    private bool isEquipped = false;

    void Start()
    {
        if (outfitObject != null)
            outfitObject.SetActive(false);

        if (equipButton != null)   equipButton.gameObject.SetActive(false);
        if (unequipButton != null) unequipButton.gameObject.SetActive(false);

        // Wire up buttons
        if (equipButton != null)   equipButton.onClick.AddListener(ReEquipItem);
        if (unequipButton != null) unequipButton.onClick.AddListener(UnequipItem);
    }

    // Called by ShopItem.BuyItem() — pass a color instead of a Sprite
    public void EquipItem(Color outfitColor)
    {
        if (outfitObject == null) return;

        lastEquippedColor = outfitColor;
        outfitObject.SetActive(true);

        // Change the outfit object's color
        var renderer = outfitObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Create a new material instance so we don't affect other objects sharing the same mat
            renderer.material = new Material(renderer.material);
            renderer.material.color = outfitColor;
        }

        isEquipped = true;
    }

    // Re-equip after unequipping (Equip button)
    void ReEquipItem()
    {
        EquipItem(lastEquippedColor);
    }

    public void UnequipItem()
    {
        Debug.Log("Unequipping item");
        if (outfitObject != null)
            outfitObject.SetActive(false);
        isEquipped = false;
    }

    // Used by NPCInteraction and PlayerHealth — same signature as before
    public bool HasClothesEquipped()
    {
        Debug.Log("Checking if clothes are equipped: " + isEquipped);
        return isEquipped;
    }
}
