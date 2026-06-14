using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopKeeperInteraction : MonoBehaviour
{
    private bool isPlayerInRange;
    private bool isShopOpen;
    public ShopUIController shopUIController;

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (isShopOpen)
                CloseShop();
            else
                OpenShop();
        }
    }

    public void OpenShop()
    {
        shopUIController.OpenShop();
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