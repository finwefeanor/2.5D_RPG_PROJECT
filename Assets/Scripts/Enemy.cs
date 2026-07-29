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
            if (Time.time >= nextAttackTime)
            {
                PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    if (enemyAttackSound != null) enemyAttackSound.Play();
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
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log("Enemy health: " + health);
        OnHealthChanged?.Invoke(health, maxHealth);
        if (enemyGetHitSound != null) enemyGetHitSound.Play();

        if (health <= 0)
            Die();
    }

    void Die()
    {
        //if (playerTransform != null)
        //{
        //    InventoryManager inv = playerTransform.GetComponent<InventoryManager>();
        //    if (inv != null) inv.AddGold(goldReward);
        //}

        //if (enemyDieSound != null) enemyDieSound.Play();
        //Destroy(gameObject);
        //Debug.Log("Enemy died: " + gameObject.name);

        if (goldPickupPrefab != null)
        {
            GameObject pickup = Instantiate(goldPickupPrefab, transform.position, Quaternion.identity);
            GoldPickup gp = pickup.GetComponent<GoldPickup>();
            if (gp != null) gp.amount = goldReward;
        }

        if (enemyDieSound != null) enemyDieSound.Play();
        Destroy(gameObject);
        Debug.Log("Enemy died: " + gameObject.name);
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