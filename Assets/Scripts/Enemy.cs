using System.Collections;
using UnityEngine;

// CHANGED FROM 2D:
//   Physics2D.OverlapCircleAll  → Physics.OverlapSphereAll
//   Collider2D[]                → Collider[]
//   Everything else identical — your health/damage/sound logic untouched

public class Enemy : MonoBehaviour
{
    public int health = 30;
    public int maxHealth = 30;

    [Header("Combat")]
    public int attackDamage = 1;
    public float attackRange = 2f;
    //public float attackCooldown = 1.2f; // matches Melee_1H_Attack_Slice_Horizontal's real length (~1.367s) — same reasoning as PlayerAttack.cs's cooldown
    public float detectRange = 5f;
    public float moveSpeed = 3f;
    public int goldReward = 5;
    public LayerMask playerLayer;
    public AudioSource enemyGetHitSound;
    public AudioSource enemyAttackSound;
    public AudioSource enemyDieSound;

    public event System.Action<int, int> OnHealthChanged;

    private float nextAttackTime = 0f;
    private Transform playerTransform;

    public GameObject goldPickupPrefab;

    public Animator animator;
    static readonly int isMovingHash = Animator.StringToHash("isMoving");

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int isDeadHash = Animator.StringToHash("isDead");
    private bool isDead = false;
    public int attackIndex = 0;

    [Tooltip("Pause AFTER the swing finishes before the next one may start. " +
         "Pure design/balance value — has nothing to do with clip length. " +
         "0 = attack continuously, 1 = one second breather between swings.")]
    public float attackRecovery = 0.4f;

    private bool isAttacking = false;   // set by EnemyAttackState, not by us

    //[Header("Directional Hit Check (later stage)")] uncommented later stage
    //public float attackAngle = 90f;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
        OnHealthChanged?.Invoke(health, maxHealth);
    }

    void Update()
    {
        if (isDead || playerTransform == null) return;

        // While the swing is playing, the animation is in charge. Don't touch anything.
        if (isAttacking) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= attackRange)
        {
            if (animator != null) animator.SetBool(isMovingHash, false);

            if (Time.time >= nextAttackTime && animator != null)
            {
                // --- LATER STAGE: directional filter ---
                // Uncomment to require the enemy be actually facing the player before attacking.
                // Useful once stuns/knockback/forced-stop states exist that could freeze
                // the enemy mid-turn while still in attackRange.
                /*
                Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dirToPlayer);
                if (angle > attackAngle / 2f)
                {
                    return; // player is behind/beside enemy — skip this attack tick
                }
                */


                animator.SetInteger(AttackIndexHash, attackIndex);
                animator.SetTrigger(AttackHash);
                // NO damage, NO sound, NO timer here — the animation handles all three.
            }
        }
        else if (distance <= detectRange)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            direction.y = 0;
            transform.position += direction * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(direction);
            if (animator != null) animator.SetBool(isMovingHash, true);
        }
        else
        {
            if (animator != null) animator.SetBool(isMovingHash, false);
        }

    }

    // --- called by EnemyAttackState (the StateMachineBehaviour) ---
    public void OnAttackAnimationStart()
    {
        isAttacking = true;
    }

    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
        nextAttackTime = Time.time + attackRecovery; // clock starts when the swing ENDS
    }

    // --- called by an Animation Event on the contact frame (frame 8) ---
    public void DealAttackDamage()
    {
        if (isDead || playerTransform == null) return;

        // Player can now dodge out of a committed swing — this is a real miss.
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance > attackRange * 1.25f) return;

        PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(attackDamage);
    }


    public void TakeDamage(int damage)
    {
        if (isDead) return; // already dead, ignore further damage entirely

        health -= damage;
        Debug.Log("Enemy health: " + health);
        OnHealthChanged?.Invoke(health, maxHealth);
        // Sound removed from here — now fires via the shared Hit_A clip's
        // Animation Event instead (PlayHitSound below), same pattern as
        // PlayAttackSound, so it stays synced with the actual flinch frame.
        if (animator != null) animator.SetTrigger(HitHash);

        if (health <= 0)
        {
            Die();
        }

    }

    // Wire via AnimationEventRelay on Skeleton_Minion — shared Hit_A clip.
    public void PlayHitSound()
    {
        if (enemyGetHitSound != null) enemyGetHitSound.Play();
    }

    // Wire via AnimationEventRelay on Skeleton_Minion — swing contact frame.
    public void PlayAttackSound()
    {
        if (enemyAttackSound != null) enemyAttackSound.Play();
    }

    void Die()
    {
        isDead = true; // always set, regardless of animator
                       // later if some AoE attack or instant kill method or other player (companion ai etc)
                       // also damages and kills enemy, therefore calls Die() method and skipping TakeDamage()
                       //method, we need to put if (isDead) return; here to prevent double enemy death.


        if (goldPickupPrefab != null)
        {
            GameObject pickup = Instantiate(goldPickupPrefab, transform.position, Quaternion.identity);
            GoldPickup gp = pickup.GetComponent<GoldPickup>();
            if (gp != null) gp.amount = goldReward;
        }

        if (enemyDieSound != null) enemyDieSound.Play();
        
        if (animator != null)
        {
            animator.SetBool(isDeadHash, true);
            animator.SetTrigger(DeathHash);
        }
        Debug.Log("Enemy died: " + gameObject.name);
        StartCoroutine(HandleEnemyDeath());
    }

    IEnumerator HandleEnemyDeath()
    {
        yield return new WaitForSeconds(2.5f); // wait for death animation to finish
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        // Red = attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Cyan = detect/chase range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}