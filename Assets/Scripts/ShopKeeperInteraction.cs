using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopKeeperInteraction : MonoBehaviour
{
    private bool isPlayerInRange;
    private bool isShopOpen;
    public ShopUIController shopUIController;

        void Start()
    {
        var ui = ShopUIRefs.Instance;
        if (ui != null && ui.shopCloseButton != null)
        {
            ui.shopCloseButton.onClick.RemoveAllListeners();
            ui.shopCloseButton.onClick.AddListener(CloseShop);
            Debug.Log("Close button wired");
        }
    }

        void OnEnable()
    {
        GameEvents.OnInteractPressed += HandleInteract;
    }

    void OnDisable()
    {
        GameEvents.OnInteractPressed -= HandleInteract;
    }

    private void HandleInteract()
    {
        if (isPlayerInRange)
        {
            ToggleShop();
        }
    }
    

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            ToggleShop();
        }
    }

    // Wire this to your on-screen Interact Button's OnClick()
    public void OnInteractButtonPressed()
    {
        if (isPlayerInRange)
            ToggleShop();
    }

    private void ToggleShop()
    {
        if (isShopOpen)
            CloseShop();
        else
            OpenShop();
    }

    public void OpenShop()
    {
        shopUIController.OpenShop();
        
        // uncomment if you want the item list to refresh every time the shop opens
        //shopManager.RefreshShopItems();
        
        isShopOpen = true;
        Debug.Log("Shop opened");
    }

    public void CloseShop()
    {
        shopUIController.CloseShop();
        isShopOpen = false;
        Debug.Log("Shop closed");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            Debug.Log("Player in range");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            // Auto close if player walks away while shop is open
            if (isShopOpen) CloseShop();
        }
    }




}