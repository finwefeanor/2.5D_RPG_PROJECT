// ── NPCInteraction.cs ────────────────────────────────────────────────────────
// CHANGED FROM 2D: OnTriggerEnter2D/Exit2D → OnTriggerEnter/Exit
// Everything else (E key, dialog logic, PlayerInventory check) identical

using UnityEngine;
using UnityEngine.UI;

public class NPCInteraction : MonoBehaviour
{
    private bool isPlayerInRange;
    public GameObject dialogBox;
    public Text dialogText;
    public string dialog;
    private PlayerInventory playerInventory;

    void Start()
    {
        playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory == null)
            Debug.LogError("PlayerInventory not found in scene.");
    }

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            dialogBox.SetActive(true);

            if (playerInventory != null)
                dialogText.text = playerInventory.HasClothesEquipped()
                    ? "You have nice clothes!"
                    : "Go buy some clothes!";
            else
                dialogText.text = "PlayerInventory not found!";
        }
    }

    // 3D trigger — removed "2D" suffix, everything else same
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player in NPC range");
            isPlayerInRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            dialogBox.SetActive(false);
        }
    }
}


// ── ShopkeeperInteraction.cs ─────────────────────────────────────────────────
// CHANGED FROM 2D: OnTriggerEnter2D/Exit2D → OnTriggerEnter/Exit
// Shop open logic completely unchanged

public class ShopkeeperInteraction : MonoBehaviour
{
    private bool isPlayerInRange;
    public ShopUIController shopUIController;

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
            shopUIController.OpenShop();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            isPlayerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            isPlayerInRange = false;
    }
}


// ── CastleInteraction.cs ─────────────────────────────────────────────────────
// CHANGED FROM 2D: OnTriggerEnter2D/Exit2D → OnTriggerEnter/Exit
// Music switching + FadeController calls completely unchanged

/*public class CastleInteraction : MonoBehaviour
{
    private bool isPlayerInRange;
    public AudioSource castleMusic;
    //public FadeController fadeController;

    private MusicManager musicManager;

    void Start()
    {
        musicManager = FindObjectOfType<MusicManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            musicManager.StopGeneralMusic();
            if (!castleMusic.isPlaying) castleMusic.Play();
            //fadeController.FadeInAndSwitchCamera(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (castleMusic.isPlaying) castleMusic.Stop();
            musicManager.generalMusic.Play();
            //fadeController.FadeInAndSwitchCamera(false);
        }
    }
}*/

