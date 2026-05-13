using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIController : MonoBehaviour
{
    public GameObject shopUI;

    void Start()
    {
        shopUI.SetActive(false); // Ensure the shop UI is hidden initially
    }

    public void OpenShop()
    {
        shopUI.SetActive(true);
    }

    public void CloseShop()
    {
        shopUI.SetActive(false);
    }
}
