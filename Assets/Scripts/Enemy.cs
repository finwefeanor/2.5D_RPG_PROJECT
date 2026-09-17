using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy AI: chase, attack, take damage, die.
///
/// Combat timing is animation-authoritative:
///   - Update() only REQUESTS an attack (sets the trigger).
///   - EnemyAttackState (StateMachineBehaviour) marks start/end via
///     OnAttackAnimationStart / OnAttackAnimationEnd.
///   - Damage and sound fire from Animation Events (contact frame 8),
///     relayed through AnimationEventRelay on Skeleton_Minion.
/// No clip lengths appear anywhere in this file.
/// </summary>
public class Enemy : MonoBehaviour
{
    // ---------------------------------------------------------------------
    // Inspector
    // ---------------------------------------------------------------------

    [Header("Health")]
    public int health = 30;
    public int maxHealth = 30;

    [Header("Combat")]
    public int attackDamage = 1;
    public float attackRange = 2f;
    public int attackIndex = 0;

    [Tooltip("Pause AFTER the swing finishes before the next one may start. " +
             "Pure design/balance value — has nothing to do with clip length. " +
             "0 = attack continuously, 1 = one second breather between swings.")]
    public float attackRecovery = 0.4f;

    // [Header("Directional Hit Check (later stage)")]
    // public float attackAngle = 90f;

    [Header("Movement & Detection")]
    public float detectRange = 5f;
    public float moveSpeed = 3f;
    public LayerMask playerLayer;

    [Header("Rewards")]
    public int goldReward = 5;
    public GameObject goldPickupPrefab;

    [Header("Refs")]
    public Animator animator;

    [Header("Audio")]
    public AudioSource enemyGetHitSound;
    public AudioSource enemyAttackSound;
    public AudioSource enemyDieSound;

    // ---------------------------------------------------------------------
    // Events
    // ---------------------------------------------------------------------


    public event Action<int, int> OnHealthChanged;

    // ---------------------------------------------------------------------
    // Runtime state
    // ---------------------------------------------------------------------

    private Transform playerTransform;
    private PlayerHealth playerHealth;   // cached — no per-hit GetComponent
    private float nextAttackTime = 0f;
    private bool isAttacking = false;    // set by EnemyAttackState, not by us
    private bool isDead = false;

    private bool playerDead = false;

    // ---------------------------------------------------------------------
    // Animator hashes
    // ---------------------------------------------------------------------

    private static readonly int isMovingHash    = Animator.StringToHash("isMoving");
    private static readonly int AttackHash      = Animator.StringToHash("Attack");
    private static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
    private static readonly int HitHash         = Animator.StringToHash("Hit");
    private static readonly int DeathHash       = Animator.StringToHash("Death");
    private static readonly int isDeadHash      = Animator.StringToHash("isDead");

    // ---------------------------------------------------------------------
    // Unity lifecycle
    // ---------------------------------------------------------------------

    void Start()
    {
        health = maxHealth;
        ResolvePlayer();
        OnHealthChanged?.Invoke(health, maxHealth);
    }

    void Update()
    {
        if (isDead || playerDead || playerTransform == null) return;

        // While the swing is playing, the animation is in charge. Don't touch anything.
        if (isAttacking) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= attackRange)      TickAttack();
        else if (distance <= detectRange) TickChase();
        else                              SetMoving(false);
    }

    // ---------------------------------------------------------------------
    // Setup
    // ---------------------------------------------------------------------


    private void ResolvePlayer()
    {
        PlayerRefs refs = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (refs == null)
        {
            Debug.LogWarning($"{name}: no Player registered — enemy will idle.", this);
            return;
        }

        playerTransform = refs.transform;
        playerHealth    = refs.Health;
    }

    /*
        It stops enemy to attack death player's corpse
    */
        void OnEnable()
    {
        GameEvents.OnPlayerDied += HandlePlayerDied;
    }

    void OnDisable()
    {
        GameEvents.OnPlayerDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        playerDead = true;
        SetMoving(false);
    }

    
        
    

    // ---------------------------------------------------------------------
    // Behaviour ticks
    // ---------------------------------------------------------------------

    private void TickAttack()
    {
        SetMoving(false);

        if (Time.time < nextAttackTime || animator == null) return;

        // --- LATER STAGE: directional filter ---
        // Uncomment to require the enemy be actually facing the player before attacking.
        // Useful once stuns/knockback/forced-stop states exist that could freeze
        // the enemy mid-turn while still in attackRange.
        /*
        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > attackAngle / 2f) return; // player is behind/beside — skip this tick
        */

        animator.SetInteger(AttackIndexHash, attackIndex);
        animator.SetTrigger(AttackHash);
        // NO damage, NO sound, NO timer here — the animation handles all three.
    }

    private void TickChase()
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0;

        transform.position += direction * moveSpeed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(direction);

        SetMoving(true);
    }

    private void SetMoving(bool moving)
    {
        if (animator != null) animator.SetBool(isMovingHash, moving);
    }

    // ---------------------------------------------------------------------
    // Animation callbacks — EnemyAttackState (StateMachineBehaviour)
    // ---------------------------------------------------------------------

    public void OnAttackAnimationStart()
    {
        isAttacking = true;
    }

    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
        nextAttackTime = Time.time + attackRecovery; // clock starts when the swing ENDS
    }

    // ---------------------------------------------------------------------
    // Animation callbacks — Animation Events via AnimationEventRelay
    // ---------------------------------------------------------------------

    /// <summary>Contact frame (frame 8 on Melee_1H_Attack_Slice_Horizontal).</summary>
    public void DealAttackDamage()
    {
        if (isDead || playerTransform == null || playerHealth == null) return;

        // Player can dodge out of a committed swing — this is a real miss.
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance > attackRange * 1.25f) return;

        playerHealth.TakeDamage(attackDamage);
    }

    /// <summary>Shared Hit_A clip — flinch frame.</summary>
    public void PlayHitSound()
    {
        if (enemyGetHitSound != null) enemyGetHitSound.Play();
    }

    /// <summary>Swing contact frame.</summary>
    public void PlayAttackSound()
    {
        if (enemyAttackSound != null) enemyAttackSound.Play();
    }

    // ---------------------------------------------------------------------
    // Damage & death
    // ---------------------------------------------------------------------

    public void TakeDamage(int damage)
    {
        if (isDead) return; // already dead, ignore further damage entirely

        health -= damage;
        OnHealthChanged?.Invoke(health, maxHealth);

        // Hit sound fires via the shared Hit_A clip's Animation Event (PlayHitSound),
        // same pattern as PlayAttackSound, so it stays synced with the flinch frame.
        if (animator != null) animator.SetTrigger(HitHash);

        if (health <= 0) Die();
    }

    private void Die()
    {
        // Guard stays even though TakeDamage checks too: future AoE / instant-kill /
        // companion-AI paths may call Die() directly, skipping TakeDamage().
        if (isDead) return;
        isDead = true;

        DropGold();

        if (enemyDieSound != null) enemyDieSound.Play();

        if (animator != null)
        {
            animator.SetBool(isDeadHash, true);
            animator.SetTrigger(DeathHash);
        }

        StartCoroutine(HandleEnemyDeath());
    }

    private void DropGold()
    {
        if (goldPickupPrefab == null) return;

        GameObject pickup = Instantiate(goldPickupPrefab, transform.position, Quaternion.identity);
        GoldPickup gp = pickup.GetComponent<GoldPickup>();
        if (gp != null) gp.amount = goldReward;
    }

    private IEnumerator HandleEnemyDeath()
    {
        yield return new WaitForSeconds(2.5f); // wait for death animation to finish
        Destroy(gameObject);
    }

    // ---------------------------------------------------------------------
    // Gizmos
    // ---------------------------------------------------------------------

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;                                    // attack range
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;                                   // detect/chase range
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}