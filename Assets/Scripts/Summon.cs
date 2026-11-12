using UnityEngine;
using UnityEngine.AI;

public class Summon : MonoBehaviour
{

    public float followDistance = 3.0f;
    public float aggroRange = 8.0f;
    public float leashRange = 12.0f;

    private float bodyRadius;

    private bool isDead = false;

    private bool playerDead = false;

    private NavMeshAgent navMeshAgent;



    [Tooltip("How fast the summon turns to face the player while in attack range (deg/sec).")]
    public float turnSpeed = 360f;

    [Header("Movement & Detection")]
    public float detectRange = 5f;
    public float moveSpeed = 3f;
    public LayerMask enemyLayer;

    private Transform playerTransform;

    [Tooltip("Only start a swing when the enemy is within this cone in front (degrees, full width). " +
             "360 = any direction.")]
    [Range(0f, 360f)]
    public float attackAngle = 60f;

    private bool isAttacking = false;    // set by EnemyAttackState, not by us

    [Header("Combat")]
    public int attackDamage = 1;
    public float attackRange = 2f;
    public int attackIndex = 0;

    [Header("Refs")]
    public Animator animator;


    private float playerRadius;
    //private PlayerHealth playerHealth;   // right now summon does not need playerHealth, maybe in later stages

    private float nextAttackTime = 0f;

    private static readonly int isMovingHash    = Animator.StringToHash("isMoving");
    private static readonly int AttackHash      = Animator.StringToHash("Attack");
    private static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
    private static readonly int HitHash         = Animator.StringToHash("Hit");
    private static readonly int DeathHash       = Animator.StringToHash("Death");
    private static readonly int isDeadHash      = Animator.StringToHash("isDead");

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bodyRadius = WorldRadius(this);

        navMeshAgent = GetComponent<NavMeshAgent>();


        ResolvePlayer();

        if (navMeshAgent != null)
        {
            navMeshAgent.speed        = moveSpeed;   // boss's slower moveSpeed still applies
            navMeshAgent.angularSpeed = turnSpeed;
            navMeshAgent.autoBraking  = true;

            // The agent measures centre-to-centre, our attackRange is edge-to-edge.
            // Stop where the bodies are attackRange apart - never inside the player.
            // Mathf.Max keeps a gap even if attackRange is 0 or negative.
            navMeshAgent.stoppingDistance =
                Mathf.Max(attackRange, 0.05f) + bodyRadius + playerRadius;
        }


    }

    // Update is called once per frame
    void Update()
    {
        if (isDead || playerDead || playerTransform == null)
        {
            StopMoving();   // otherwise the agent keeps walking to its last destination
            return;
        }

        // While the swing is playing, the animation is in charge. Don't touch anything.
        if (isAttacking) return;


        //float distance = Vector3.Distance(transform.position, playerTransform.position);
        float distance = EdgeDistanceToPlayer();

        if (distance <= attackRange)      TickAttack();
        else if (distance <= detectRange) TickChase();
        else                              StopMoving();
    }

    private void TickAttack()
    {
        StopMoving();
        FacePlayer();   // keep turning toward the player while in range (not mid-swing:
                        // Update() returns early while isAttacking, so swings stay committed)

        if (Time.time < nextAttackTime || animator == null) return;

        // Only swing when roughly facing the player - e.g. right after a flinch,
        // or when the player has circled behind.
        Vector3 dirToEnemy = playerTransform.position - transform.position;
        dirToEnemy.y = 0;
        if (Vector3.Angle(transform.forward, dirToEnemy) > attackAngle / 2f) return;

        animator.SetInteger(AttackIndexHash, attackIndex);
        animator.SetTrigger(AttackHash);
        // NO damage, NO sound, NO timer here — the animation handles all three.
    }

    private void FacePlayer()
    {
        Vector3 dir = playerTransform.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
    }

    private void TickChase()
    {

        if (navMeshAgent == null || !navMeshAgent.isOnNavMesh) return;

        navMeshAgent.isStopped = false;


        navMeshAgent.SetDestination(playerTransform.position);


        SetMoving(true);

    }

    private float EdgeDistanceToPlayer()
    {
        float centres = Vector3.Distance(transform.position, playerTransform.position);
        return centres - bodyRadius - playerRadius;
    }

        private static float WorldRadius(Component c)
    {
        CapsuleCollider capsule = c.GetComponent<CapsuleCollider>();
        if (capsule == null) return 0f;
        Vector3 s = capsule.transform.lossyScale;
        return capsule.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
    }

        private void ResolvePlayer()
    {
        PlayerRefs refs = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (refs == null)
        {
            Debug.LogWarning($"{name}: no Player registered — enemy will idle.", this);
            return;
        }

        playerTransform = refs.transform;
        //playerHealth    = refs.Health;
        playerRadius    = WorldRadius(refs);
    }

        private void StopMoving()
    {
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();
            navMeshAgent.velocity  = Vector3.zero;   // halt now - no sliding into the player
        }
        SetMoving(false);

    }

        private void SetMoving(bool moving)
    {
        if (animator != null) animator.SetBool(isMovingHash, moving);
    }

}
