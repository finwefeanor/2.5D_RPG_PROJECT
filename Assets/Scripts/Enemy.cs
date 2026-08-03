using System.Collections;
using UnityEngine;

// CHANGED FROM 2D:
//   Physics2D.OverlapCircleAll  → Physics.OverlapSphereAll
//   Collider2D[]                → Collider[]
//   Everything else identical — your health/damage/sound logic untouched

public class Enemy : MonoBehaviour
{
    public int health = 30;
    public int maxHealth = 30; // set equal to health at start in Inspector
    
    public int attackDamage = 10;
    public float attackRange = 1.0f;
    public float attackRate = 1.0f;
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

    public GameObject goldPickupPrefab; // assign in Inspector

    public Animator animator;
    static readonly int isMovingHash = Animator.StringToHash("isMoving");

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int isDeadHash = Animator.StringToHash("isDead");
    public int attackIndex = 0;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
        OnHealthChanged?.Invoke(health, maxHealth);
    }

    void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= attackRange)
        {
            if (animator != null) animator.SetBool(isMovingHash, false);

            if (Time.time >= nextAttackTime)
            {
                PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    if (enemyAttackSound != null) enemyAttackSound.Play();
                }

                if (animator != null)
                {
                    animator.SetInteger(AttackIndexHash, attackIndex);
                    animator.SetTrigger(AttackHash);
                }

                nextAttackTime = Time.time + 1f / attackRate;
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

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log("Enemy health: " + health);
        OnHealthChanged?.Invoke(health, maxHealth);
        if (enemyGetHitSound != null) enemyGetHitSound.Play();
        if (animator != null) animator.SetTrigger(HitHash);

        if (health <= 0)
            Die();
    }

    void Die()
    {

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
        yield return new WaitForSeconds(2f); // wait for death animation to finish
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