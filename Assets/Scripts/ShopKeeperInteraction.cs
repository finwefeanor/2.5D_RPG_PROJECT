using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopKeeperInteraction : MonoBehaviour
{
    private bool isPlayerInRange;
    public GameObject shopUI; // Reference to the shop UI GameObject
    public ShopUIController shopUIController; // Reference to the ShopUIController script

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            OpenShop();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            // Optionally show a prompt (e.g., "Press E to interact")
            //this.transform.rotation = Quaternion.Euler(new Vector3(0, 180, 0)); /
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            // Optionally hide the prompt
        }
        //this.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
    }

    void OpenShop()
    {
        // Use the ShopUIController to open the shop UI
        shopUIController.OpenShop();
        // Optionally, you could also directly activate the shop UI if you prefer
        // shopUI.SetActive(true);
    }
}