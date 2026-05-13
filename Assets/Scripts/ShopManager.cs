using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public TextMeshProUGUI playerGoldText; // Ensure this is TextMeshProUGUI
    public int playerGold = 100;
    public GameObject itemButtonPrefab; // Prefab for the item button
    public Transform shopContent; // Content parent for the shop items

    // These might be missing or not initialized correctly
    public Sprite itemIcon; // Example sprite for icon, should be assigned in inspector
    public Sprite itemSprite; // Example sprite for item, should be assigned in inspector


    void Start()
    {
        UpdatePlayerGold();
        PopulateShop();
    }

    public void UpdatePlayerGold()
    {
        playerGoldText.text = "Your Gold: " + playerGold.ToString();
    }

    void PopulateShop()
    {
        // Example items
        AddShopItem("Hat", 20, itemIcon, itemSprite);
        AddShopItem("Shirt", 30, itemIcon, itemSprite);
    }

    void AddShopItem(string name, int price, Sprite icon, Sprite sprite)
    {
        GameObject itemButton = Instantiate(itemButtonPrefab, shopContent);
        ShopItem shopItem = itemButton.GetComponent<ShopItem>();
        shopItem.itemName = name;
        shopItem.price = price;
        shopItem.itemIcon = icon;
        //shopItem.itemSprite = sprite;
        shopItem.player = FindObjectOfType<PlayerController>().gameObject;
        itemButton.GetComponentInChildren<TextMeshProUGUI>().text = name + " - " + price + "G";

        // Ensure the button triggers the BuyItem method
        itemButton.GetComponent<Button>().onClick.AddListener(shopItem.BuyItem);
    }
}