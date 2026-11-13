using UnityEngine;
using UnityEngine.AI;

// The Mage's summoned skeleton. Spawned by PlayerSummons; runs EnemyAnimationController
// through the same AnimationEventRelay + EnemyAttackState as Enemy (ICombatAnimationReceiver).
//   no target -> stand near the player; follow once the player walks away
//   target    -> nearest living Enemy near this summon AND near the player (leash)
//   in range  -> stop, face it, swing. Damage lands on the clip's contact event.
// v1: enemies ignore summons and summons have no health. They leave when their time is up.
public class Summon : MonoBehaviour, ICombatAnimationReceiver
{

    [Header("Follow")]
    [Tooltip("Start following when the player is further than this (metres).")]
    public float followDistance = 3f;

    [Tooltip("Radius of the ring around the player where idle summons stand. One slot each.")]
    public float followRadius = 1.8f;

    [Tooltip("Agent speed. The player runs at 5, so keep it close or summons fall behind.")]
    public float moveSpeed = 4.5f;

    public float turnSpeed = 360f;

    [Header("Targeting")]
    [Tooltip("Enemies further than this from the summon are not noticed.")]
    public float aggroRange = 8f;

    [Tooltip("Enemies further than this from the PLAYER are ignored or dropped, so summons stay close.")]
    public float leashRange = 12f;

    [Tooltip("Seconds between target searches while idle. Not every frame - cheaper on phones.")]
    public float retargetInterval = 0.3f;

    [Tooltip("Layer the enemies are on. Set to Enemy.")]
    public LayerMask enemyLayer;

    [Header("Combat")]
    [Tooltip("Edge-to-edge distance at which it starts swinging (same meaning as Enemy.attackRange).")]
    public float attackRange = 0.4f;
    public int attackIndex = 0;
    public float attackRecovery = 0.4f;
    [Range(0f, 360f)] public float attackAngle = 60f;
    [Range(0f, 360f)] public float hitAngle = 120f;
    public float dodgeMargin = 0.5f;

    [Header("Despawn")]
    [Tooltip("Seconds the death clip plays before the object is removed.")]
    public float despawnDelay = 2.5f;

    [Header("Refs (inside this prefab)")]
    public Animator animator;
    public AudioSource attackSound;

    public int Slot { get; private set; }   // follow position around the player, from PlayerSummons

    private PlayerSummons owner;
    private Transform playerTransform;
    private NavMeshAgent agent;
    private int damage;
    private float despawnTime;
    private float bodyRadius;

    private Enemy target;
    private float targetRadius;
    private float nextRetargetTime;
    private float nextAttackTime;
    private bool isAttacking;        // set by EnemyAttackState, not by us
    private bool following;
    private bool leaving;            // lifetime over or despawned: playing the death clip
    private bool hasDestination;
    private Vector3 lastDestination;


    private static readonly int IsMovingHash    = Animator.StringToHash("isMoving");
    private static readonly int AttackHash      = Animator.StringToHash("Attack");
    private static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
    private static readonly int DeathHash       = Animator.StringToHash("Death");
    private static readonly int IsDeadHash      = Animator.StringToHash("isDead");

    

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        bodyRadius = WorldRadius(this);
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        if (agent != null)
        {
            agent.speed        = moveSpeed;   // boss's slower moveSpeed still applies
            agent.angularSpeed = turnSpeed;
            agent.autoBraking  = true;
            agent.stoppingDistance = 0.1f;   // Update stops it at attackRange (edge distance) itself
        }

    }

      // Called by PlayerSummons right after Instantiate (after Awake, before Start).
    public void Init(PlayerSummons owner, int damage, float lifetime, int slot)
    {
        this.owner = owner;
        this.damage = damage;
        this.despawnTime = Time.time + lifetime;
        this.Slot = slot;
        this.playerTransform = owner.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (leaving) return;
        if (Time.time >= despawnTime|| playerTransform == null)
        {
            Despawn();   // otherwise the agent keeps walking to its last destination
            return;
        }

        // While the swing is playing, the animation is in charge.
        if (isAttacking) return;

        // Keep the current target until it dies or leaves the leash; search only when idle.
        if (target != null && !IsValidTarget(target)) target = null;
        if (target == null && Time.time >= nextRetargetTime)
        {
            nextRetargetTime = Time.time + retargetInterval;
            FindTarget();
        }


        if (target != null)
        {
            following = false;
            if (EdgeDistanceTo(target.transform, targetRadius) <= attackRange) TickAttack();
            else MoveTo(target.transform.position);
        }
        else
        {
            TickFollow();
        }
    }

    // ---------------------------------------------------------------------
    // Targeting
    // ---------------------------------------------------------------------

    private bool IsValidTarget(Enemy e)
    {
        if (e == null || e.IsDead) return false;
        return FlatSqrDistance(e.transform.position, playerTransform.position) <= leashRange * leashRange;
    }

    private void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, aggroRange, enemyLayer,
                                                QueryTriggerInteraction.Ignore);
        Enemy best = null;
        float bestSqr = float.MaxValue;

        foreach (Collider c in hits)
        {
            Enemy e = c.GetComponentInParent<Enemy>();
            if (!IsValidTarget(e)) continue;

            float sqr = FlatSqrDistance(e.transform.position, transform.position);
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = e;
            }
        }

        target = best;
        targetRadius = best != null ? WorldRadius(best) : 0f;
    }

    // ---------------------------------------------------------------------
    // Behaviour ticks
    // ---------------------------------------------------------------------

    private void TickAttack()
    {
        StopMoving();
        Vector3 dir = Flat(target.transform.position - transform.position);
        FaceDirection(dir);

        if (Time.time < nextAttackTime || animator == null) return;
        if (dir.sqrMagnitude > 0.0001f && Vector3.Angle(transform.forward, dir) > attackAngle / 2f) return;

        animator.SetInteger(AttackIndexHash, attackIndex);
        animator.SetTrigger(AttackHash);
        // NO damage, NO sound, NO timer here - the animation handles all three.
    }

    // Stand still until the player is followDistance away, then walk to this summon's slot.
    private void TickFollow()
    {
        float toPlayer = Mathf.Sqrt(FlatSqrDistance(transform.position, playerTransform.position));
        if (!following && toPlayer > followDistance) following = true;
        if (!following)
        {
            StopMoving();
            return;
        }

        Vector3 slot = SlotPosition();
        MoveTo(slot);

        if (ArrivedAt(slot))
        {
            following = false;
            StopMoving();
        }
    }

    // 4 slots on the diagonals around the player; slots 4+ go on a wider, rotated ring.
    // World-fixed angles: summons don't reshuffle every time the player turns.
    private Vector3 SlotPosition()
    {
        int ring = Slot / 4;
        float angle = 45f + Slot * 90f + ring * 45f;
        float radius = followRadius * (1f + ring * 0.6f);
        return playerTransform.position + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
    }

    // Close to the slot, or as close as the NavMesh allows (slot inside a wall).
    private bool ArrivedAt(Vector3 point)
    {
        if (FlatSqrDistance(transform.position, point) <= 0.3f * 0.3f) return true;
        return agent != null && agent.isOnNavMesh && hasDestination && !agent.pathPending &&
               agent.remainingDistance <= agent.stoppingDistance + 0.2f;
    }

    // ---------------------------------------------------------------------
    // Movement
    // ---------------------------------------------------------------------

    private void MoveTo(Vector3 destination)
    {
        if (agent == null || !agent.isOnNavMesh) return;

        agent.isStopped = false;
        // Re-path only when the goal moved noticeably: cheaper, and pathPending can settle.
        if (!hasDestination || (destination - lastDestination).sqrMagnitude > 0.25f)
        {
            agent.SetDestination(destination);
            lastDestination = destination;
            hasDestination = true;
        }
        SetMoving(true);
    }

    private void StopMoving()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity  = Vector3.zero;   // halt now - no sliding into the player
        }
        hasDestination = false;
        SetMoving(false);

    }

    private void SetMoving(bool moving)
    {
        if (animator != null) animator.SetBool(IsMovingHash, moving);
    }

    private void FaceDirection(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
    }

    // ---------------------------------------------------------------------
    // ICombatAnimationReceiver (EnemyAttackState + AnimationEventRelay)
    // ---------------------------------------------------------------------

    public void OnAttackAnimationStart()
    {
        isAttacking = true;
    }

    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
        nextAttackTime = Time.time + attackRecovery;   // clock starts when the swing ENDS
    }

    // Contact frame. Same miss rules as Enemy: the target can step out of a committed swing.
    public void DealAttackDamage()
    {
        if (leaving || target == null || target.IsDead) return;
        if (EdgeDistanceTo(target.transform, targetRadius) > attackRange + dodgeMargin) return;

        Vector3 dir = Flat(target.transform.position - transform.position);
        if (dir.sqrMagnitude > 0.0001f && Vector3.Angle(transform.forward, dir) > hitAngle / 2f) return;

        target.TakeDamage(damage);
    }

    public void PlayAttackSound()
    {
        if (attackSound != null) attackSound.Play();
    }

    public void PlayHitSound() { }   // v1: summons are never hit

    // ---------------------------------------------------------------------
    // Leaving
    // ---------------------------------------------------------------------

    // Lifetime over, cap pushed it out, or the player died. Plays the death clip, then removed.
    public void Despawn()
    {
        if (leaving) return;
        leaving = true;

        if (owner != null) owner.Remove(this);   // frees its slot and cap place right away
        StopMoving();
        if (agent != null) agent.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;

        if (animator != null)
        {
            animator.SetBool(IsDeadHash, true);
            animator.SetTrigger(DeathHash);
        }
        Destroy(gameObject, despawnDelay);
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------
    private float EdgeDistanceTo(Transform other, float otherRadius)
    {
        return Mathf.Sqrt(FlatSqrDistance(transform.position, other.position)) - bodyRadius - otherRadius;
    }


    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    private static float FlatSqrDistance(Vector3 a, Vector3 b) => Flat(a - b).sqrMagnitude;

    private static float WorldRadius(Component c)
    {
        CapsuleCollider capsule = c.GetComponent<CapsuleCollider>();
        if (capsule == null) return 0f;
        Vector3 s = capsule.transform.lossyScale;
        return capsule.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
    }

}
