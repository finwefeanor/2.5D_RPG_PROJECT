using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Player health: armour mitigation, flinch, death.
// Lives on the Player root. The hurt sound normally comes from the Hit_A clip's
// PlayHitSound Animation Event (so it matches the visible flinch); when a hit does
// NOT flinch, TakeDamage plays it directly instead.
public class PlayerHealth : MonoBehaviour
{
    public int health = 100;
    public int maxHealth = 100;
    public AudioSource playerHitSound;
    public AudioSource playerDieSound;
    // Health UI listens to GameEvents.OnPlayerHealthChanged (the old per-player event was unused).

    [Header("Flinch (stun-lock protection)")]
    [Tooltip("After a flinch, further hits still deal damage but don't play Hit_A again for " +
             "this long. Stops a crowd from stun-locking the player. 0 = flinch on every hit.")]
    public float flinchImmunity = 1.5f;

    [Tooltip("Off = hits taken during an attack animation never interrupt it (hyper armour); " +
             "the damage still lands. On = any hit can cancel a swing.")]
    public bool flinchWhileAttacking = false;

    private float nextFlinchTime = 0f;

    [Header("Death UI")]
    private GameObject deathScreenUI; // resolved from PlayerUIRefs in Start

    private bool isDead = false;

    private EquipmentManager equipmentManager;
    private PlayerAttack playerAttack;

    [SerializeField] private Animator animator;

    // Called by PlayerClass after it spawns the class's visual.
    public void SetAnimator(Animator a) => animator = a;

    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int isDeadHash = Animator.StringToHash("isDead");

    void Start()
    {
        equipmentManager = GetComponent<EquipmentManager>();
        playerAttack = GetComponent<PlayerAttack>();

        var ui = PlayerUIRefs.Instance;
        if (ui != null) deathScreenUI = ui.deathScreenPanel;
        else Debug.LogWarning("PlayerHealth: no PlayerHealthBarCanvas in scene.", this);

        GameEvents.PlayerHealthChanged(health, maxHealth); // initial value for UI
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        int defense = equipmentManager != null ? equipmentManager.GetTotalDefense() : 0;
        int mitigated = Mathf.Max(1, damage - defense);

        health -= mitigated;
        GameEvents.PlayerHealthChanged(health, maxHealth);

        if (health <= 0)
        {
            Die();
            return;
        }

        // Damage always lands; the FLINCH is limited, the same way Enemy limits it:
        //  - never mid-swing (unless flinchWhileAttacking), so the player can fight back,
        //  - at most once per flinchImmunity seconds, so two skeletons can't chain-stun.
        bool attacking = playerAttack != null && playerAttack.IsAttacking;
        bool canFlinch = Time.time >= nextFlinchTime && (flinchWhileAttacking || !attacking);

        if (canFlinch && animator != null)
        {
            animator.SetTrigger(HitHash);   // Hit_A's PlayHitSound event plays the sound
            nextFlinchTime = Time.time + flinchImmunity;
        }
        else
        {
            PlayHitSound();                 // no flinch animation -> play the hurt sound here
        }
    }

    // Hit_A clip, flinch frame (through PlayerAnimationEventRelay). Also called by
    // TakeDamage for hits that don't flinch.
    public void PlayHitSound()
    {
        if (playerHitSound != null) playerHitSound.Play();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        var refs = GetComponent<PlayerRefs>();
        if (refs.Controller != null) refs.Controller.enabled = false;
        if (refs.Attack != null) refs.Attack.enabled = false;

        if (animator != null)
        {
            animator.SetBool(isDeadHash, true);
            animator.SetTrigger(DeathHash);
        }

        GameEvents.PlayerDied();

        StartCoroutine(HandleDeath());
    }

    IEnumerator HandleDeath()
    {
        if (deathScreenUI != null) deathScreenUI.SetActive(true);

        if (playerDieSound != null) playerDieSound.Play();

        float waitTime = playerDieSound != null && playerDieSound.clip != null ? playerDieSound.clip.length : 6.0f;
        yield return new WaitForSecondsRealtime(waitTime);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
