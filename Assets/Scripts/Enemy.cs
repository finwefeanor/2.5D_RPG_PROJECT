using UnityEngine;

// CHANGED FROM 2D:
//   Physics2D.OverlapCircleAll  → Physics.OverlapSphereAll
//   Collider2D[]                → Collider[]
//   Everything else identical — your health/damage/sound logic untouched

public class Enemy : MonoBehaviour
{
    public int health = 30;
    public int attackDamage = 10;
    public float attackRange = 1.0f;
    public float attackRate = 1.0f;
    public LayerMask playerLayer;
    public AudioSource enemyGetHitSound;
    public AudioSource enemyAttackSound;
    public AudioSource enemyDieSound;

    private float nextAttackTime = 0f;

    void Update()
    {
        if (Time.time >= nextAttackTime)
        {
            // 3D overlap sphere instead of 2D overlap circle
            Collider[] hitPlayers = Physics.OverlapSphere(transform.position, attackRange, playerLayer);

            foreach (Collider player in hitPlayers)
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    if (enemyAttackSound != null) enemyAttackSound.Play();
                }
            }

            nextAttackTime = Time.time + 1f / attackRate;
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log("Enemy health: " + health);
        if (enemyGetHitSound != null) enemyGetHitSound.Play();

        if (health <= 0)
            Die();
    }

    void Die()
    {
        if (enemyDieSound != null) enemyDieSound.Play();
        Destroy(gameObject);
        Debug.Log("Enemy died: " + gameObject.name);
    }

    void OnDrawGizmosSelected()
    {
        // DrawWireSphere works the same in 3D — no change needed
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
