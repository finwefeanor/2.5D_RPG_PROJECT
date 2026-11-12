using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerAttack : MonoBehaviour
{
    public int baseAttackDamage = 10;
    public Transform attackPoint;   // child empty GameObject placed in front of player

    public LayerMask enemyLayers;   // set on the Player prefab (Enemy layer) - never in code

    [Tooltip("Where projectiles spawn. Empty child of the Player root, in front at chest height.")]
    [SerializeField] private Transform firePoint;



    [Tooltip("Melee only: full arc in front of the player that can be hit. " +
             "120 = 60 deg left/right of forward. 360 = all around.")]
    [Range(0f, 360f)]
    public float meleeHitAngle = 120f;



    public AudioSource attackSound;


    [Tooltip("Random pitch offset per swing (±). 0.05 = ±5%. Stops repeated hits sounding robotic.")]
    [Range(0f, 0.2f)]
    public float pitchVariation = 0.05f;

    public ParticleSystem attackEffect;
    [SerializeField] private Animator animator;

    private EquipmentManager equipmentManager;
    private PlayerController controller;
    private PlayerClass playerClass;

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
    private static readonly int AttackSpeedHash = Animator.StringToHash("AttackSpeed");
    private static readonly int ComboNextHash = Animator.StringToHash("ComboNext");

    [Header("Attack")]
    [Tooltip("Character-wide attack speed. Multiplies every ability's animationSpeed. " +
             "Later this comes from class stats / items (e.g. +10% attack speed ring).")]
    [Range(0.25f, 3f)]
    public float attackSpeed = 1f;

    [Tooltip("Pause AFTER the swing finishes before the next one may start. " +
             "Pure design/balance value - nothing to do with clip length. " +
             "0 = swing again immediately, 0.5 = half-second breather.")]
    public float attackRecovery = 0.15f;

    [Tooltip("Action queue: an attack / skill pressed while busy (swing, recovery, jump) plays as " +
             "soon as the player is free - if the press is at most this many seconds old. Long " +
             "enough to cover a whole swing (JumpChop 1.33 s), short enough that a forgotten press " +
             "doesn't fire much later. Only ONE action is queued: the newest press wins.")]
    public float queueTime = 2f;

    private float bufferedPressTime = -1f;   // < 0 = nothing remembered

    // --- Class skill ---
    private float skillReadyTime = 0f;       // cooldown ends at this Time.time
    private float bufferedSkillTime = -1f;   // skill pressed mid-swing / mid-jump

    // How many attack states are playing. Normally 0 or 1; it is 2 for a moment while one
    // combo step crossfades into the next (Unity enters the new state BEFORE exiting the old).
    private int attackStatesActive = 0;
    public bool IsAttacking => attackStatesActive > 0;   // read by PlayerHealth (no flinch mid-swing)
    private float nextAttackTime = 0f;

    // Snapshot taken when the swing starts. The contact-frame event reads this,
    // so equipping/unequipping mid-swing can't change which attack lands.
    private AbilityDefinition activeAttack;

    // --- Combo state ---
    private AbilityDefinition comboRoot;   // first attack of the current chain (weapon / unarmed head)
    private int currentSwingHash;          // the newest attack state - the one whose time counts
    private float swingTime;               // its normalized time (0-1), fed by PlayerAttackState
    private bool comboQueued;              // a press was remembered during this swing
    private bool chainPending;             // ComboNext sent, waiting for the next state to enter
    private bool contactDone;              // this swing's DealAttackDamage already fired (cancel guard)


    
    void Start()
    {
        equipmentManager = GetComponent<EquipmentManager>();
        controller = GetComponent<PlayerController>();
        playerClass = GetComponent<PlayerClass>();
    }

    

    void Update()
    {
        // Space is jump now. Attack: J, or left mouse on PC (not on phones - a touch is also a
        // mouse click there - and not when the click lands on UI like the joystick or buttons).
        bool mouseAttack = !Application.isMobilePlatform && Input.GetMouseButtonDown(0) && !PointerOverUI();
        if (Input.GetKeyDown(KeyCode.J) || mouseAttack)
            TryAttack();
        else
            TryBufferedAttack();

        // Class skill: Q, or right mouse on PC (same UI / phone rules as the attack).
        bool mouseSkill = !Application.isMobilePlatform && Input.GetMouseButtonDown(1) && !PointerOverUI();
        if (Input.GetKeyDown(KeyCode.Q) || mouseSkill)
            TrySkill();
        else
            TryBufferedSkill();
    }

    private static bool PointerOverUI() =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    // No attacks from the jump press until landing; the press is buffered instead,
    // so attacking just before touching down swings on landing.
    private bool JumpBlocksAttack => controller != null && controller.IsBusyJumping;

    // Fires a remembered press once attacking is allowed again - only if the press is
    // recent, so an old press can't fire a surprise shot seconds later.
    private void TryBufferedAttack()
    {
        if (bufferedPressTime < 0f || IsAttacking || Time.time < nextAttackTime || JumpBlocksAttack) return;

        bool fresh = Time.time - bufferedPressTime <= queueTime;
        bufferedPressTime = -1f;
        if (fresh) TryAttack();
    }

    // Wired to the on-screen AttackButton's OnClick()
    public void OnAttackButtonPressed()
    {
        TryAttack();
    }

    void OnEnable()
    {
        GameEvents.OnAttackPressed += OnAttackButtonPressed;
        GameEvents.OnSkillPressed += TrySkill;
    }

    void OnDisable()
    {
        GameEvents.OnAttackPressed -= OnAttackButtonPressed;
        GameEvents.OnSkillPressed -= TrySkill;
    }

    // The skill comes from the CLASS, not the weapon - so it works with any weapon in hand.
    private AbilityDefinition CurrentSkill =>
        playerClass != null && playerClass.Definition != null ? playerClass.Definition.skill : null;

    // Q / right mouse / SKILL button. Same pipeline as a basic attack (animation is the clock,
    // contact-frame event, delivery branch) - only the ability's source and the cooldown differ.
    public void TrySkill()
    {
        AbilityDefinition skill = CurrentSkill;
        if (skill == null) return;
        if (Time.time < skillReadyTime) return;   // on cooldown: ignored (the button shows it)

        // Busy (swing, recovery, jump): remember it and cast as soon as it's allowed.
        if (IsAttacking || Time.time < nextAttackTime || JumpBlocksAttack)
        {
            bufferedSkillTime = Time.time;
            bufferedPressTime = -1f;   // newest press wins
            return;
        }

        bufferedSkillTime = -1f;
        bufferedPressTime = -1f;   // the skill replaces a remembered basic attack
        comboRoot = null;          // skills never chain (TryChainCombo drops the chain)
        skillReadyTime = Time.time + skill.cooldown;
        StartAttack(skill, AttackHash);
        GameEvents.SkillUsed(skill.cooldown);
    }

    private void TryBufferedSkill()
    {
        if (bufferedSkillTime < 0f || IsAttacking || Time.time < nextAttackTime || JumpBlocksAttack) return;

        bool fresh = Time.time - bufferedSkillTime <= queueTime;
        bufferedSkillTime = -1f;
        if (fresh) TrySkill();
    }


    private void TryAttack()
    {
        // Mid-swing the animation is in charge.
        if (IsAttacking)
        {
            // Attack with a combo: the press may chain (QueueComboInput's rules).
            // Attack without one (crossbow): remember it for when the swing is over.
            if (activeAttack != null && activeAttack.nextInCombo != null) QueueComboInput();
            else { bufferedPressTime = Time.time; bufferedSkillTime = -1f; }   // newest press wins
            return;
        }
        if (Time.time < nextAttackTime || JumpBlocksAttack)
        {
            bufferedPressTime = Time.time;   // recovery or mid-jump: fire when it's allowed
            bufferedSkillTime = -1f;         // newest press wins
            return;
        }

        AbilityDefinition attack = equipmentManager != null ? equipmentManager.CurrentAttack : null;
        if (attack == null) return;   // no weapon attack AND no unarmed default assigned

        bufferedPressTime = -1f;
        comboRoot = attack;           // a fresh press always starts at the head of the chain
        StartAttack(attack, AttackHash);
    }

    // Shared by a fresh attack (Attack trigger) and a combo step (ComboNext trigger).
    private void StartAttack(AbilityDefinition attack, int triggerHash)
    {
        activeAttack = attack;
        comboQueued = false;
        contactDone = false;   // the new swing hasn't hit yet

        if (animator != null)
        {
            animator.SetFloat(AttackSpeedHash, attack.animationSpeed * attackSpeed);
            animator.SetInteger(AttackIndexHash, attack.animatorIndex);
            animator.SetTrigger(triggerHash);
        }

        if (attackEffect != null) attackEffect.Play();

        // NO damage, NO sound, NO timer here - the animation drives all three.
    }

    private void QueueComboInput()
    {
        if (chainPending || activeAttack == null || activeAttack.nextInCombo == null) return;
        if (swingTime < activeAttack.comboInputOpens) return;   // too early: mashing, ignored

        comboQueued = true;
        TryChainCombo();   // already past comboChainAt? chain right now
    }

    // Runs on a press and every frame of the swing: fires the queued step once the swing
    // reaches comboChainAt (always after the contact frame, so this swing's hit still lands).
    private void TryChainCombo()
    {
        if (!comboQueued || chainPending || activeAttack == null) return;
        if (swingTime < activeAttack.comboChainAt) return;

        // Weapon swapped mid-combo: the chain belongs to the old weapon, drop it.
        AbilityDefinition head = equipmentManager != null ? equipmentManager.CurrentAttack : null;
        if (head != comboRoot)
        {
            comboQueued = false;
            return;
        }

        chainPending = true;
        StartAttack(activeAttack.nextInCombo, ComboNextHash);
    }

    // Called by PlayerClass after it spawns the class's visual.
    public void SetAnimator(Animator a) => animator = a;

    // Called by PlayerClass on a runtime class switch: the old Animator is destroyed,
    // so its OnStateExit never arrives. Clear everything a swing could have left behind.
    public void ResetAttackState()
    {
        attackStatesActive = 0;
        comboQueued        = false;
        chainPending       = false;
        swingTime          = 0f;
        activeAttack       = null;
        nextAttackTime     = 0f;
        bufferedPressTime  = -1f;   // a press made before a class switch must not fire after it
        bufferedSkillTime  = -1f;
        skillReadyTime     = 0f;    // new class, new skill: start ready
        contactDone        = false;
    }

    // --- called by PlayerAttackState (the StateMachineBehaviour) ---
    public void OnAttackAnimationStart(int stateHash)
    {
        attackStatesActive++;
        currentSwingHash = stateHash;
        swingTime = 0f;
        chainPending = false;
    }

    public void OnAttackAnimationUpdate(int stateHash, float normalizedTime)
    {
        // During a combo crossfade BOTH attack states update; only the newest one counts.
        if (stateHash != currentSwingHash || chainPending) return;

        swingTime = normalizedTime;
        TryChainCombo();
        TryCancelIntoQueued();
    }

    // Cancel window: a queued skill / attack cuts this swing's follow-through instead of
    // waiting for the clip's end. Never before the hit (contactDone), never before cancelFrom,
    // and never into the SAME clip (a state crossfading into itself would make swingTime
    // jump between the old and new copy) - repeating an attack waits for the normal end.
    private void TryCancelIntoQueued()
    {
        if (chainPending || !contactDone || activeAttack == null) return;
        if (swingTime < activeAttack.cancelFrom) return;

        AbilityDefinition next = null;
        bool isSkill = false;
        if (bufferedSkillTime >= 0f && Time.time - bufferedSkillTime <= queueTime &&
            CurrentSkill != null && Time.time >= skillReadyTime)
        {
            next = CurrentSkill;
            isSkill = true;
        }
        else if (bufferedPressTime >= 0f && Time.time - bufferedPressTime <= queueTime)
        {
            next = equipmentManager != null ? equipmentManager.CurrentAttack : null;
        }
        if (next == null || next.animatorIndex == activeAttack.animatorIndex) return;

        bufferedPressTime = -1f;
        bufferedSkillTime = -1f;
        chainPending = true;   // same handshake as a combo step
        if (isSkill)
        {
            comboRoot = null;
            skillReadyTime = Time.time + next.cooldown;
            StartAttack(next, ComboNextHash);
            GameEvents.SkillUsed(next.cooldown);
        }
        else
        {
            comboRoot = next;      // a fresh basic attack starts its own chain
            StartAttack(next, ComboNextHash);
        }
    }

    public void OnAttackAnimationEnd()
    {
        attackStatesActive = Mathf.Max(0, attackStatesActive - 1);
        if (attackStatesActive > 0) return;   // old half of a combo crossfade - the chain goes on

        // The chain is over: finished normally, or interrupted (Hit_A, death).
        comboQueued = false;
        chainPending = false;
        if (animator != null) animator.ResetTrigger(ComboNextHash);   // never let it fire later
        nextAttackTime = Time.time + attackRecovery; // clock starts when the LAST swing ends
    }

     // --- called by an Animation Event on the contact frame ---
    // The clip decides WHEN, the ability's delivery decides HOW.
    public void DealAttackDamage()
    {

        if (activeAttack == null) return;
        contactDone = true;   // the hit is out - the cancel window may open now

        int bonus = equipmentManager != null ? equipmentManager.GetTotalDamageBonus() : 0;
        int totalDamage = Mathf.RoundToInt(baseAttackDamage * activeAttack.damageMultiplier) + bonus;

        if (activeAttack.delivery == AbilityDelivery.Projectile)
        {
            FireProjectile(activeAttack, totalDamage);
            return;   // the arrow plays the hit sound when (if) it lands
        }

        if (attackPoint == null) return;

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, activeAttack.range, enemyLayers);

        bool hitSomething = false;

        foreach (Collider enemy in hitEnemies)
        {
            // Front arc only. Flat direction + Vector3.Angle = a real angle, independent of
            // distance. An enemy standing inside the player (zero vector) always counts.

           Vector3 toEnemy = enemy.transform.position - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude > 0.0001f &&
                Vector3.Angle(transform.forward, toEnemy) > meleeHitAngle / 2f)
                continue;

            Enemy enemyComponent = enemy.GetComponent<Enemy>();
            if (enemyComponent != null)
            {
                enemyComponent.TakeDamage(totalDamage);
                hitSomething = true;
            }

        }

        // Once per swing, not once per enemy - a cleave through 3 skeletons is one impact.
        if (hitSomething) PlayHitSound(activeAttack);

    }

        private void FireProjectile(AbilityDefinition ability, int damage)
    {
        if (ability.projectilePrefab == null)
        {
            Debug.LogWarning($"{ability.name}: delivery is Projectile but projectilePrefab is empty.");
            return;
        }

        Transform origin = firePoint != null ? firePoint : transform;

        // Flat forward: the arrow flies parallel to the ground at fire-point height.
        Vector3 forward = transform.forward;
        forward.y = 0f;

        // Rotation is set by Launch (direction + the prefab's model offset).
        ArrowProjectile arrow = Instantiate(ability.projectilePrefab, origin.position, Quaternion.identity);
        arrow.Launch(this, ability, damage, forward);
    }

    // Called by ArrowProjectile on an enemy hit. The arrow may land after the next swing
    // started, so it passes its own ability instead of us reading activeAttack.
    public void OnProjectileHit(AbilityDefinition ability) => PlayHitSound(ability);

    // Contact-frame event: the SWING sound, hit or miss.
    // The AudioSource's own clip is an impact sound, so no fallback here.
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
    private void PlayHitSound(AbilityDefinition ability)
    {
        if (attackSound == null) return;

        AudioClip hit = ability != null ? AbilityDefinition.Pick(ability.hitSounds) : null;
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


