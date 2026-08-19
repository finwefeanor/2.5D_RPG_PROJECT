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
    [Tooltip("0 = Slice_Horizontal, 1 = Stab - temporary manual override until combo/weapon logic exists")]
    public int attackIndex = 0;

    [Tooltip("Pause AFTER the swing finishes before the next one may start. " +
             "Pure design/balance value - nothing to do with clip length. " +
             "0 = swing again immediately, 0.5 = half-second breather.")]
    public float attackRecovery = 0.15f;

    private bool isAttacking = false;   // set by PlayerAttackState, not by us
    private float nextAttackTime = 0f;

    /*
    // --- Directional attack settings (disabled for now - see DealAttackDamage) ---
    [Header("Directional Hit Check (later stage)")]
    [Tooltip("Full cone angle in degrees. 90 = must be within 45 deg left/right of forward.")]
    public float attackAngle = 90f;
    */

    void Start()
    {
        equipmentManager = GetComponent<EquipmentManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            TryAttack();
    }

    // Wired to the on-screen AttackButton's OnClick()
    public void OnAttackButtonPressed()
    {
        TryAttack();
    }

    private void TryAttack()
    {
        // While the swing is playing, the animation is in charge - ignore input.
        if (isAttacking) return;
        if (Time.time < nextAttackTime) return;

        if (animator != null)
        {
            animator.SetInteger(AttackIndexHash, attackIndex);
            animator.SetTrigger(AttackHash);
        }

        if (attackEffect != null) attackEffect.Play();

        // NO damage, NO sound, NO timer here - the animation drives all three.
    }

    // --- called by PlayerAttackState (the StateMachineBehaviour) ---
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
        if (attackPoint == null) return;

        int bonus = equipmentManager != null ? equipmentManager.GetTotalDamageBonus() : 0;
        int totalDamage = baseAttackDamage + bonus;

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider enemy in hitEnemies)
        {
            // --- LATER STAGE: directional filter ---
            // Uncomment to require the enemy be roughly in front of the player.
            // AoE/whirlwind attacks would call a separate method that skips this.
            /*
            Vector3 dirToEnemy = (enemy.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToEnemy);
            if (angle > attackAngle / 2f) continue;
            */

            Enemy enemyComponent = enemy.GetComponent<Enemy>();
            if (enemyComponent != null)
                enemyComponent.TakeDamage(totalDamage);
        }
    }

    // --- called by an Animation Event on the contact frame (frame 8) ---
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


