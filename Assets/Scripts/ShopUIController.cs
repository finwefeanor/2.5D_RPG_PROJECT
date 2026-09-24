using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIController : MonoBehaviour
{
    private GameObject shopUI;

    void Start()
    {
        var refs = ShopUIRefs.Instance;
        if (refs == null)
        {
            Debug.LogError("ShopUIController: no ShopCanvas in scene.", this);
            return;
        }

        shopUI = refs.shopPanel;
        shopUI.SetActive(false);
    }

    public void OpenShop()  { if (shopUI != null) shopUI.SetActive(true);  }
    public void CloseShop() { if (shopUI != null) shopUI.SetActive(false); }


}
