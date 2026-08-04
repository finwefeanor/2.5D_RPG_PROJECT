using UnityEngine;

// CHANGED FROM 2D:
//   Physics2D.OverlapCircleAll  → Physics.OverlapSphereAll
//   Collider2D[]                → Collider[]
//   ParticleSystem still works fine in 3D — no change
//   attackPoint: assign a child empty GameObject in front of the player


public class PlayerAttack : MonoBehaviour
{
    public int baseAttackDamage = 10;
    public float attackRange = 1.5f;
    public Transform attackPoint;   // child empty GameObject placed in front of player
    public LayerMask enemyLayers;
    public AudioSource attackSound;
    public ParticleSystem attackEffect;
    [SerializeField] private Animator animator;

    private EquipmentManager equipmentManager;

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");

    [Header("Attack")]
    [Tooltip("0 = Slice_Horizontal, 1 = Stab — temporary manual override until combo/weapon logic exists")]
    public int attackIndex = 0;
    public float attackCooldown = 1.2f; // slightly under clip length (1.367s) so next attack queues right as swing finishes
    private float nextAttackTime = 0f;


    void Start()
    {
        equipmentManager = GetComponent<EquipmentManager>(); // adjust if it lives elsewhere
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    void Attack()
    {
        if (animator != null)
        {
            animator.SetInteger(AttackIndexHash, attackIndex); // 0 = Slice_Horizontal, 1 = Stab, etc.
            animator.SetTrigger(AttackHash);
        }

        if (attackSound != null) attackSound.Play();
        if (attackEffect != null) attackEffect.Play();

        int bonus = equipmentManager != null ? equipmentManager.GetTotalDamageBonus() : 0;
        int totalDamage = baseAttackDamage + bonus;

        // 3D sphere overlap instead of 2D circle overlap
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);
        Debug.Log("Enemies detected: " + hitEnemies.Length);


        foreach (Collider enemy in hitEnemies)
        {
            Debug.Log("Damaging enemy: " + enemy.name);
            Enemy enemyComponent = enemy.GetComponent<Enemy>();
            if (enemyComponent != null)
            {
                enemyComponent.TakeDamage(totalDamage);
                Debug.Log("Enemy took damage: " + totalDamage);
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
