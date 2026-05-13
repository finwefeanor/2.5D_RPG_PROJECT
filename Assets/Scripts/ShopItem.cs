using UnityEngine;
using UnityEngine.UI;

// CHANGED FROM 2D:
//   Sprite itemSprite → Color outfitColor
//   Everything else (gold deduction, button wiring, isPurchased) identical

public class ShopItem : MonoBehaviour
{
    public string itemName;
    public int price;
    public Sprite itemIcon;         // still used for shop UI button icon — no change
    public Color outfitColor = Color.cyan; // replaces Sprite itemSprite for 3D equipping
    public GameObject player;

    private ShopManager shopManager;
    private bool isPurchased = false;

    void Start()
    {
        shopManager = FindObjectOfType<ShopManager>();
    }

    public void BuyItem()
    {
        if (!isPurchased && shopManager.playerGold >= price)
        {
            shopManager.playerGold -= price;
            shopManager.UpdatePlayerGold();
            EquipItem();
            isPurchased = true;
            ShowEquipUnequipButtons();
        }
    }

    void EquipItem()
    {
        // Pass color to PlayerInventory instead of a Sprite
        player.GetComponent<PlayerInventory>().EquipItem(outfitColor);
    }

    void ShowEquipUnequipButtons()
    {
        Button equipButton   = player.GetComponent<PlayerInventory>().equipButton;
        Button unequipButton = player.GetComponent<PlayerInventory>().unequipButton;
        if (equipButton != null)   equipButton.gameObject.SetActive(true);
        if (unequipButton != null) unequipButton.gameObject.SetActive(true);
    }
}
