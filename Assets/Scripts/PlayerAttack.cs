using UnityEngine;

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
    public float attackCooldown = 1.2f;
    private float nextAttackTime = 0f;

    // --- Directional attack settings (disabled for now — see Attack() below) ---
    /*
    [Header("Directional Hit Check (later stage)")]
    [Tooltip("Full cone angle in degrees. 90 = must be within 45° left/right of forward)]
    public float attackAngle = 90f;
    */

    void Start()
    {
        equipmentManager = GetComponent<EquipmentManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextAttackTime)
        {
            TryAttack();
        }
    }

    public void OnAttackButtonPressed()
    {
        TryAttack();
    }

    private void TryAttack()
    {
        if (Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    void Attack()
    {
        if (animator != null)
        {
            animator.SetInteger(AttackIndexHash, attackIndex);
            animator.SetTrigger(AttackHash);
        }

        // Sound removed from here — now fires via Animation Event at the
        // swing's contact frame instead (see PlayAttackSound() below),
        // same pattern as Enemy.cs, so it stays synced even if the swing
        // gets interrupted (e.g. player gets hit mid-attack).

        if (attackEffect != null) attackEffect.Play();

        int bonus = equipmentManager != null ? equipmentManager.GetTotalDamageBonus() : 0;
        int totalDamage = baseAttackDamage + bonus;

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);
        Debug.Log("Enemies detected: " + hitEnemies.Length);

        foreach (Collider enemy in hitEnemies)
        {
            // --- LATER STAGE: directional filter ---
            /*
            Vector3 dirToEnemy = (enemy.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToEnemy);
            if (angle > attackAngle / 2f)
            {
                continue; // enemy is outside the frontal cone — skip it
            }
            */

            Debug.Log("Damaging enemy: " + enemy.name);
            Enemy enemyComponent = enemy.GetComponent<Enemy>();
            if (enemyComponent != null)
            {
                enemyComponent.TakeDamage(totalDamage);
                Debug.Log("Enemy took damage: " + totalDamage);
            }
        }
    }

    // Wire this to an Animation Event on the attack clip's contact frame
    // (already exists on Melee_1H_Attack_Slice_Horizontal at frame 8 —
    // no new event needed if you're using that same clip).
    public void PlayAttackSound()
    {
        if (attackSound != null) attackSound.Play();
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}