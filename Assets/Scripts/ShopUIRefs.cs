// ShopUIRefs.cs — on the ShopCanvas root
using UnityEngine;
using UnityEngine.UI;

public class ShopUIRefs : MonoBehaviour
{
    public static ShopUIRefs Instance { get; private set; }

    [Header("Assign inside the ShopCanvas prefab")]
    public GameObject shopPanel;
    public Text goldText;
    public Transform itemButtonContainer;
    public GameObject itemButtonPrefab;
    public Button shopCloseButton;


    [Header("Inventory UI")]
    public GameObject inventoryPanel;
    public Transform inventoryRowContainer;
    public GameObject inventoryRowPrefab;   // can stay the project asset in Prefabs/
    public Button inventoryCloseButton;

    void Awake() => Instance = this;
}