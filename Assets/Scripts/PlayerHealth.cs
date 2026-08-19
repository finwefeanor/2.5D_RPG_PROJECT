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

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        Debug.Log($"TakeDamage called at {Time.time:F2}s, damage={damage}"); // ADD THIS

        int defense = equipmentManager != null ? equipmentManager.GetTotalDefense() : 0;
        int mitigated = Mathf.Max(1, damage - defense);

        health -= mitigated;
        Debug.Log("Player health: " + health);
        OnHealthChanged?.Invoke(health, maxHealth);

        // Sound removed from here — now fires via Animation Event on the
        // Hit reaction clip instead (see PlayHitSound() below), so it's
        // guaranteed to sync with the actual visual flinch, not a code timer.
        if (animator != null) animator.SetTrigger(HitHash);

        if (health <= 0)
            Die();
    }

    // Wire this to an Animation Event on your Hit reaction clip
    // (same pattern as Enemy's PlayAttackSound — may need an
    // AnimationEventRelay on the Animator's GameObject if it's
    // a separate child object, e.g. MagePlayerVisual).
    public void PlayHitSound()
    {
        Debug.Log($"PlayHitSound (animation event) fired at {Time.time:F2}s"); // ADD THIS
        if (playerHitSound != null) playerHitSound.Play();
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
        if (deathScreenUI != null) deathScreenUI.SetActive(true);

        Time.timeScale = 0f;

        if (playerDieSound != null) playerDieSound.Play();

        float waitTime = playerDieSound != null ? playerDieSound.clip.length : 3.0f;
        yield return new WaitForSecondsRealtime(waitTime);

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
