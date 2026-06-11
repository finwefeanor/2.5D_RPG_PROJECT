// ============================================================
//  ShopManager.cs  —  Assets/Scripts/
//
//  CHANGED FROM ORIGINAL 2D VERSION:
//    Removed itemButtonPrefab — buttons are pre-built by ShopUIBuilder,
//    no runtime instantiation needed.
//    goldText is still wired automatically by ShopUIBuilder.
// ============================================================
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [Header("Player Gold")]
    public int playerGold = 100;

    [Header("UI — auto-wired by ShopUIBuilder")]
    public Text playerGoldText;

    void Start()
    {
        UpdatePlayerGold();
    }

    public void UpdatePlayerGold()
    {
        if (playerGoldText != null)
            playerGoldText.text = "Gold: " + playerGold;
        else
            Debug.LogWarning("ShopManager: goldText not assigned. " +
                             "Run RPG Scene Builder > Build Shop UI to wire it.");
    }
}