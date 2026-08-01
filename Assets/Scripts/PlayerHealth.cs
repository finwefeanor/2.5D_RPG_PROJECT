using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// CHANGED FROM 2D:
//   OnTriggerEnter2D  → OnTriggerEnter
//   OnTriggerExit2D   → OnTriggerExit  (not used here but good habit)
//   SpriteRenderer clothesRenderer → PlayerInventory reference
//     In 3D, "has armor" is tracked by PlayerInventory.HasClothesEquipped()
//     instead of checking if a SpriteRenderer is active

public class PlayerHealth : MonoBehaviour
{
    public int health = 100;
    //public int armorReduction = 5; clean it later
    public AudioSource playerHitSound;
    public AudioSource playerDieSound;
    public int maxHealth = 100;
    public event System.Action<int, int> OnHealthChanged; // current, max

    [Header("Death UI")]
    public GameObject deathScreenUI; // simple full-screen panel with "You Died" text, assign in Inspector

    private bool isDead = false;

    private PlayerInventory playerInventory;
    private EquipmentManager equipmentManager;

    [SerializeField] private Animator animator;

    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int isDeadHash = Animator.StringToHash("isDead");

    void Start()
    {
        equipmentManager = GetComponent<EquipmentManager>();
        OnHealthChanged?.Invoke(health, maxHealth); // initial value for UI
    }

    // 3D trigger — same logic, just no "2D" suffix
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
            TakeDamage(20);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return; // ignore all damage once death is already triggered
        // Armor check via PlayerInventory instead of SpriteRenderer
        int defense = equipmentManager != null ? equipmentManager.GetTotalDefense() : 0;


        int mitigated = Mathf.Max(1, damage - defense); // floor at 1, armor can't grant immunity


        health -= mitigated;
        Debug.Log("Player health: " + health);
        OnHealthChanged?.Invoke(health, maxHealth);

        if (playerHitSound != null) playerHitSound.Play();

        if (playerHitSound != null) playerHitSound.Play();
        if (animator != null) animator.SetTrigger(HitHash); // only fires if we didn't early-return above

        if (health <= 0)
            Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("Player has died");
        if (animator != null)
        {
            animator.SetBool(isDeadHash, true);
            animator.SetTrigger(DeathHash);
        }
        StartCoroutine(HandleDeath());
    }

    IEnumerator HandleDeath()
    {
        //if (deathScreenUI != null) deathScreenUI.SetActive(true);

        //SaveSystem.Save(GetComponent<InventoryManager>(), GetComponent<EquipmentManager>());


        //if (playerDieSound != null)
        //    playerDieSound.Play();

        //yield return new WaitForSeconds(
        //    playerDieSound != null ? playerDieSound.clip.length : 3.0f);

        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        if (deathScreenUI != null) deathScreenUI.SetActive(true);

        Time.timeScale = 0f; // freezes enemy movement, attacks, and player input-driven physics

        if (playerDieSound != null) playerDieSound.Play(); // audio isn't affected by timeScale

        float waitTime = playerDieSound != null ? playerDieSound.clip.length : 3.0f;
        yield return new WaitForSecondsRealtime(waitTime); // must be *Realtime* since timeScale is 0

        Time.timeScale = 1f; // reset before reload, or the new scene loads frozen
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
