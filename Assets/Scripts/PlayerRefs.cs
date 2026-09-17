using UnityEngine;
// PlayerRefs.cs — sits on the Player root, caches its own components.
// This is the ONLY file you edit when you add a new player component.
public class PlayerRefs : MonoBehaviour
{
    public PlayerHealth Health { get; private set; }
    public PlayerAttack Attack { get; private set; }
    public PlayerController Controller { get; private set; }
    public InventoryManager Inventory { get; private set; }
    public Animator Animator { get; private set; }

    public EquipmentManager Equipment { get; private set; }

// PlayerRefs.cs — registration moves to Awake
void Awake()
{
    Health     = GetComponent<PlayerHealth>();
    Attack     = GetComponent<PlayerAttack>();
    Controller = GetComponent<PlayerController>();
    Inventory  = GetComponent<InventoryManager>();
    Animator   = GetComponentInChildren<Animator>();

    GameManager.Instance.RegisterPlayer(this);   // lazy getter finds GM even if its Awake hasn't run
    
    Equipment = GetComponent<EquipmentManager>();
}

    // PlayerRefs.cs — temporary verification
    void Start()
    {
        Debug.Log($"[PlayerRefs] Registered. Health={Health != null}, Attack={Attack != null}, " +
                $"Controller={Controller != null}, Inventory={Inventory != null}, Animator={Animator != null}");
    }
    
}
