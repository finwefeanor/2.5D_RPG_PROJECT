using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

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
    public int maxHealth = 30;
    [System.NonSerialized] public int health;

    [Header("Combat")]
    public int attackDamage = 1;
    public float attackRange = 2f;
    public int attackIndex = 0;

    [Tooltip("After flinching, further hits still deal damage but don't restart Hit_A " +
             "for this long. Gives the enemy a window to fight back. 0 = flinch on every hit.")]
    public float flinchImmunity = 1.2f;

    [Tooltip("Heavy enemies / bosses: never play Hit_A at all, only take damage.")]
    public bool neverFlinch = false;

    private float nextFlinchTime = 0f;

    [Tooltip("Pause AFTER the swing finishes before the next one may start. " +
             "Pure design/balance value — has nothing to do with clip length. " +
             "0 = attack continuously, 1 = one second breather between swings.")]
    public float attackRecovery = 0.4f;

    [Tooltip("Only start a swing when the player is within this cone in front (degrees, full width). " +
             "360 = any direction.")]
    [Range(0f, 360f)]
    public float attackAngle = 60f;

    [Tooltip("Full width (degrees) of the swing's hit arc at the contact frame. " +
             "Player outside it when the hit lands = dodged. 360 = full circle (spin / boss sweep).")]
    [Range(0f, 360f)]
    public float hitAngle = 120f;

    [Tooltip("Extra distance (beyond attackRange) at which a committed swing still connects. " +
             "Smaller = easier to dodge by stepping back mid-swing.")]
    public float dodgeMargin = 0.5f;

    [Tooltip("How fast the enemy turns to face the player while in attack range (deg/sec).")]
    public float turnSpeed = 360f;

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

    private NavMeshAgent navMeshAgent;

    // ---------------------------------------------------------------------
    // Events
    // ---------------------------------------------------------------------


    public event Action<int, int> OnHealthChanged;

    // ---------------------------------------------------------------------
    // Runtime state
    // ---------------------------------------------------------------------

    private Transform playerTransform;
    private PlayerHealth playerHealth;   // cached — no per-hit GetComponent

    private float bodyRadius;     // own capsule radius in world units - grows with scale
    private float playerRadius;   // player's capsule radius in world units
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
        bodyRadius = WorldRadius(this);

        navMeshAgent = GetComponent<NavMeshAgent>();


        ResolvePlayer();

        if (navMeshAgent != null)
        {
            navMeshAgent.speed        = moveSpeed;   // boss's slower moveSpeed still applies
            navMeshAgent.angularSpeed = turnSpeed;
            navMeshAgent.autoBraking  = true;

            // The agent measures centre-to-centre, our attackRange is edge-to-edge.
            // Stop where the bodies are attackRange apart - never inside the player.
            // Mathf.Max keeps a gap even if attackRange is 0 or negative.
            navMeshAgent.stoppingDistance =
                Mathf.Max(attackRange, 0.05f) + bodyRadius + playerRadius;
        }


        OnHealthChanged?.Invoke(health, maxHealth);
    }

    void Update()
    {
        if (isDead || playerDead || playerTransform == null)
        {
            StopMoving();   // otherwise the agent keeps walking to its last destination
            return;
        }

        // While the swing is playing, the animation is in charge. Don't touch anything.
        if (isAttacking) return;


        //float distance = Vector3.Distance(transform.position, playerTransform.position);
        float distance = EdgeDistanceToPlayer();

        if (distance <= attackRange)      TickAttack();
        else if (distance <= detectRange) TickChase();
        else                              StopMoving();
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
        playerRadius    = WorldRadius(refs);
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
        StopMoving();
        FacePlayer();   // keep turning toward the player while in range (not mid-swing:
                        // Update() returns early while isAttacking, so swings stay committed)

        if (Time.time < nextAttackTime || animator == null) return;

        // Only swing when roughly facing the player - e.g. right after a flinch,
        // or when the player has circled behind.
        Vector3 dirToPlayer = playerTransform.position - transform.position;
        dirToPlayer.y = 0;
        if (Vector3.Angle(transform.forward, dirToPlayer) > attackAngle / 2f) return;

        animator.SetInteger(AttackIndexHash, attackIndex);
        animator.SetTrigger(AttackHash);
        // NO damage, NO sound, NO timer here — the animation handles all three.
    }

    private void TickChase()
    {

        if (navMeshAgent == null || !navMeshAgent.isOnNavMesh) return;

        navMeshAgent.isStopped = false;


        navMeshAgent.SetDestination(playerTransform.position);


        SetMoving(true);

    }

    // Halts the agent in place and tells the Animator we're idle.
    private void StopMoving()
    {
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();
            navMeshAgent.velocity  = Vector3.zero;   // halt now - no sliding into the player
        }
        SetMoving(false);

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

        // Player stepped back out of a committed swing — a real miss.
        float distance = EdgeDistanceToPlayer();
        if (distance > attackRange + dodgeMargin) return;

        // Player sidestepped / got behind the swing — also a miss. 360 = hits all around.
        Vector3 dirToPlayer = playerTransform.position - transform.position;
        dirToPlayer.y = 0;
        if (Vector3.Angle(transform.forward, dirToPlayer) > hitAngle / 2f) return;

        playerHealth.TakeDamage(attackDamage);
    }

    private void FacePlayer()
    {
        Vector3 dir = playerTransform.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
    }

    private static float WorldRadius(Component c)
    {
        CapsuleCollider capsule = c.GetComponent<CapsuleCollider>();
        if (capsule == null) return 0f;
        Vector3 s = capsule.transform.lossyScale;
        return capsule.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
    }

    // Gap between the two bodies' edges, not their centres.
    private float EdgeDistanceToPlayer()
    {
        float centres = Vector3.Distance(transform.position, playerTransform.position);
        return centres - bodyRadius - playerRadius;
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

        if (health <= 0)
        {
            Die();
            return;
        }

        // Damage always lands; the FLINCH is rate-limited, so a combo can't lock the
        // enemy in Hit_A forever. Inside the immunity window it keeps attacking.
        // Hit sound fires via the Hit_A clip's Animation Event, so no flinch = no hurt sound
        // (the player's weapon hitSound still plays from PlayerAttack).
        if (!neverFlinch && Time.time >= nextFlinchTime && animator != null)
        {
            animator.SetTrigger(HitHash);
            nextFlinchTime = Time.time + flinchImmunity;
        }
    }

    private void Die()
    {
        // Guard stays even though TakeDamage checks too: future AoE / instant-kill /
        // companion-AI paths may call Die() directly, skipping TakeDamage().
        if (isDead) return;
        isDead = true;
        StopMoving();

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