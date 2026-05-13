using UnityEngine;

// CHANGED FROM 2D:
//   Physics2D.OverlapCircleAll  → Physics.OverlapSphereAll
//   Collider2D[]                → Collider[]
//   ParticleSystem still works fine in 3D — no change
//   attackPoint: assign a child empty GameObject in front of the player

public class PlayerAttack : MonoBehaviour
{
    public int attackDamage = 10;
    public float attackRange = 1.5f;
    public Transform attackPoint;   // child empty GameObject placed in front of player
    public LayerMask enemyLayers;
    public AudioSource attackSound;
    public ParticleSystem attackEffect;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            Attack();
    }

    void Attack()
    {
        if (attackSound != null) attackSound.Play();
        if (attackEffect != null) attackEffect.Play();

        // 3D sphere overlap instead of 2D circle overlap
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);
        Debug.Log("Enemies detected: " + hitEnemies.Length);

        foreach (Collider enemy in hitEnemies)
        {
            Debug.Log("Damaging enemy: " + enemy.name);
            Enemy enemyComponent = enemy.GetComponent<Enemy>();
            if (enemyComponent != null)
            {
                enemyComponent.TakeDamage(attackDamage);
                Debug.Log("Enemy took damage: " + attackDamage);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
