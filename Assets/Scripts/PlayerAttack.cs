using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public int baseAttackDamage = 10;
    public Transform attackPoint;   // child empty GameObject placed in front of player
    public LayerMask enemyLayers;
    public AudioSource attackSound;

    [Tooltip("Random pitch offset per swing (±). 0.05 = ±5%. Stops repeated hits sounding robotic.")]
    [Range(0f, 0.2f)]
    public float pitchVariation = 0.05f;

    public ParticleSystem attackEffect;
    [SerializeField] private Animator animator;

    private EquipmentManager equipmentManager;

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
    private static readonly int AttackSpeedHash = Animator.StringToHash("AttackSpeed");

    [Header("Attack")]
    [Tooltip("Character-wide attack speed. Multiplies every ability's animationSpeed. " +
             "Later this comes from class stats / items (e.g. +10% attack speed ring).")]
    [Range(0.25f, 3f)]
    public float attackSpeed = 1f;

    [Tooltip("Pause AFTER the swing finishes before the next one may start. " +
             "Pure design/balance value - nothing to do with clip length. " +
             "0 = swing again immediately, 0.5 = half-second breather.")]
    public float attackRecovery = 0.15f;

    private bool isAttacking = false;   // set by PlayerAttackState, not by us
    private float nextAttackTime = 0f;

    // Snapshot taken when the swing starts. The contact-frame event reads this,
    // so equipping/unequipping mid-swing can't change which attack lands.
    private AbilityDefinition activeAttack;

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

    void OnEnable()
    {
        GameEvents.OnAttackPressed += OnAttackButtonPressed;
    }

    void OnDisable()
    {
        GameEvents.OnAttackPressed -= OnAttackButtonPressed;
    }

    private void TryAttack()
    {
        // While the swing is playing, the animation is in charge - ignore input.
        if (isAttacking) return;
        if (Time.time < nextAttackTime) return;

        AbilityDefinition attack = equipmentManager != null ? equipmentManager.CurrentAttack : null;
        if (attack == null) return;   // no weapon attack AND no unarmed default assigned

        activeAttack = attack;

        if (animator != null)
        {
            animator.SetFloat(AttackSpeedHash, attack.animationSpeed * attackSpeed);
            animator.SetInteger(AttackIndexHash, attack.animatorIndex);
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
        if (attackPoint == null || activeAttack == null) return;

        int bonus = equipmentManager != null ? equipmentManager.GetTotalDamageBonus() : 0;
        int totalDamage = Mathf.RoundToInt(baseAttackDamage * activeAttack.damageMultiplier) + bonus;

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, activeAttack.range, enemyLayers);

        bool hitSomething = false;

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
            {
                enemyComponent.TakeDamage(totalDamage);
                hitSomething = true;
            }

        }

        // Once per swing, not once per enemy - a cleave through 3 skeletons is one impact.
        if (hitSomething) PlayHitSound();

    }

    // --- called by an Animation Event on the contact frame (frame 8) ---
        // Contact-frame event: the SWING sound, hit or miss.
    // The AudioSource's own clip is an impact sound, so no fallback here.
    // Contact-frame event: the SWING sound, hit or miss.
    public void PlayAttackSound()
    {
        if (attackSound == null || activeAttack == null) return;

        // One random pitch per swing. The hit sound fires on the same frame and shares it,
        // so changing pitch here can't bend a sound that's already playing.
        attackSound.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);

        AudioClip swing = AbilityDefinition.Pick(activeAttack.swingSounds);
        if (swing != null) attackSound.PlayOneShot(swing);
    }

    // Called from DealAttackDamage only when something was hit.
    // Called from DealAttackDamage only when something was hit.
    private void PlayHitSound()
    {
        if (attackSound == null) return;

        AudioClip hit = activeAttack != null ? AbilityDefinition.Pick(activeAttack.hitSounds) : null;
        if (hit != null) attackSound.PlayOneShot(hit);
        else attackSound.Play();   // fallback: AudioSource's own clip (sword-slice)
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        // equipmentManager is only cached in Start(), so resolve it here for edit mode.
        EquipmentManager eq = equipmentManager != null ? equipmentManager : GetComponent<EquipmentManager>();
        AbilityDefinition a = eq != null ? eq.CurrentAttack : null;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position, a != null ? a.range : 1.5f);
    }
}


