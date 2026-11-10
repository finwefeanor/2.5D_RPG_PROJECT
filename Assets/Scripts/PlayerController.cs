using UnityEngine;

// ============================================================
//  PlayerController.cs  -  Player root
//  Movement for every class: walk / run (one ratio for all
//  classes), turning, and a physics jump.
//
//  Speeds:   moveSpeed is the RUN speed (set by PlayerClass from
//            the class). Walk = moveSpeed * walkSpeedRatio.
//            Caps Lock toggles walk / run. The joystick is analog
//            on top of that: a half tilt moves at half speed.
//  Animator: MoveSpeed (m/s) drives the Locomotion blend tree
//            (Idle 0 / Walk ~2.2 / Run ~5). IsGrounded + Jump
//            drive Jump_Start -> Jump_Idle -> Jump_Land.
//  Jump:     the animation is the clock. RequestJump() only plays
//            Jump_Start (crouch); its JumpTakeoff event (frame 9)
//            calls JumpTakeoff(), which launches the Rigidbody.
//            No jumping while attacking, no attacking in the air.
// ============================================================
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("RUN speed in m/s. Overwritten by PlayerClass from the class definition.")]
    public float moveSpeed = 5f;

    [Tooltip("Walk speed as a fraction of run speed, the same for every class.")]
    [Range(0.1f, 1f)]
    public float walkSpeedRatio = 0.45f;

    [Tooltip("Start in run mode. Caps Lock toggles walk / run.")]
    public bool startRunning = true;

    public float turnSpeed = 720f;   // degrees per second

    [Header("Jump")]
    [Tooltip("Peak height of a jump in metres.")]
    public float jumpHeight = 1.2f;

    [Tooltip("Gravity multiplier while airborne. >1 = snappier, less floaty jump.")]
    public float gravityMultiplier = 2f;

    [Tooltip("How fast you can change horizontal velocity in the air (m/s per second).")]
    public float airControl = 12f;

    [Tooltip("What counts as ground. Default excludes Player, Enemy, Ignore Raycast and UI.")]
    public LayerMask groundLayers = ~((1 << 2) | (1 << 5) | (1 << 6) | (1 << 7));

    [Tooltip("Radius of the ground probe at the feet.")]
    public float groundCheckRadius = 0.3f;

    [Tooltip("Walking off an edge only counts as falling after this many seconds (no anim flicker on bumps).")]
    public float coyoteTime = 0.12f;

    [Tooltip("If Jump_Start's JumpTakeoff event never arrives (e.g. a flinch cut the crouch), the jump " +
             "request is dropped after this long.")]
    public float takeoffTimeout = 0.6f;

    [Header("Animation (optional)")]
    public Animator animator;           // set by PlayerClass.BindVisual

    [Tooltip("Smoothing time (s) for the Animator's MoveSpeed float. 0 = instant.")]
    public float speedDampTime = 0.1f;

    [HideInInspector] public bool rootMotionActive = false; // set by CharacterMotor

    // --- state ---
    private Rigidbody rb;
    private PlayerAttack playerAttack;
    private VirtualJoystick joystick;
    private Vector3 movementDirection;   // magnitude 0..1

    private bool isRunning;
    private bool grounded = true;
    private float lastGroundedTime;
    private bool jumpPending;            // Jump_Start playing, waiting for JumpTakeoff
    private float jumpPendingSince;
    private bool leftGroundByJump;       // airborne because of a jump (not a fall)
    private float ignoreGroundUntil;     // right after takeoff the probe still touches the floor

    public bool IsRunning => isRunning;
    public bool IsGrounded => grounded;
    // True from the jump press until landing. PlayerAttack / PlayerHealth read this.
    public bool IsBusyJumping => jumpPending || !grounded;

    static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    static readonly int JumpHash = Animator.StringToHash("Jump");
    static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    void Awake()
    {
        isRunning = startRunning;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation; // the capsule must not tip over
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        playerAttack = GetComponent<PlayerAttack>();
        joystick = VirtualJoystick.Instance;
        lastGroundedTime = Time.time;
    }

    void OnEnable()  { GameEvents.OnJumpPressed += RequestJump; }
    void OnDisable() { GameEvents.OnJumpPressed -= RequestJump; }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (joystick != null && Mathf.Abs(h) < 0.01f && Mathf.Abs(v) < 0.01f)
        {
            h = joystick.Horizontal;
            v = joystick.Vertical;
        }

        // Keyboard diagonals are 1.41 long, the joystick gives 0..1: clamp to 1 so diagonals
        // are not faster and stick tilt sets the speed.
        movementDirection = Vector3.ClampMagnitude(new Vector3(h, 0f, v), 1f);

        if (Input.GetKeyDown(KeyCode.CapsLock)) isRunning = !isRunning;
        if (Input.GetKeyDown(KeyCode.Space)) RequestJump();

        // Crouch was interrupted (flinch, death...) and the takeoff event never came.
        if (jumpPending && Time.time - jumpPendingSince > takeoffTimeout)
        {
            jumpPending = false;
            if (animator != null) animator.ResetTrigger(JumpHash);
        }

        if (animator != null)
        {
            float speed = grounded ? movementDirection.magnitude * CurrentSpeed : 0f;
            animator.SetFloat(MoveSpeedHash, speed, speedDampTime, Time.deltaTime);
            // Coyote time: a short step off a ledge doesn't flash the falling pose.
            bool animGrounded = grounded ||
                                (!leftGroundByJump && Time.time - lastGroundedTime < coyoteTime);
            animator.SetBool(IsGroundedHash, animGrounded);
        }
    }

    private float CurrentSpeed => moveSpeed * (isRunning ? 1f : walkSpeedRatio);

    // Space / JUMP button. Only starts the crouch; JumpTakeoff (animation event) launches.
    public void RequestJump()
    {
        if (!enabled || !grounded || jumpPending || rootMotionActive) return;
        if (playerAttack != null && playerAttack.IsAttacking) return;   // finish the swing first

        jumpPending = true;
        jumpPendingSince = Time.time;
        if (animator != null) animator.SetTrigger(JumpHash);
        else JumpTakeoff();   // no animator: jump right away
    }

    // Jump_Start clip, frame 9 (end of the crouch) - through PlayerAnimationEventRelay.
    public void JumpTakeoff()
    {
        if (!jumpPending) return;
        jumpPending = false;

        float g = -Physics.gravity.y * gravityMultiplier;
        Vector3 vel = rb.linearVelocity;
        vel.y = Mathf.Sqrt(2f * g * jumpHeight);   // v0 that reaches jumpHeight under gravity g
        rb.linearVelocity = vel;

        grounded = false;
        leftGroundByJump = true;
        ignoreGroundUntil = Time.time + 0.15f;
    }

    void FixedUpdate()
    {
        UpdateGrounded();
        if (rootMotionActive) return; // let CharacterMotor drive the Rigidbody instead

        if (movementDirection.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(movementDirection);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, target, turnSpeed * Time.fixedDeltaTime));
        }

        Vector3 desired = movementDirection * CurrentSpeed;
        Vector3 vel = rb.linearVelocity;
        Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);

        if (grounded)
            horizontal = desired;   // full control on the ground
        else
        {
            horizontal = Vector3.MoveTowards(horizontal, desired, airControl * Time.fixedDeltaTime);
            vel.y += Physics.gravity.y * (gravityMultiplier - 1f) * Time.fixedDeltaTime;   // extra gravity
        }

        rb.linearVelocity = new Vector3(horizontal.x, vel.y, horizontal.z);
    }

    private void UpdateGrounded()
    {
        bool touching = Time.time >= ignoreGroundUntil &&
                        Physics.CheckSphere(transform.position + Vector3.up * (groundCheckRadius - 0.1f),
                                            groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
        // Still rising = not landed yet, even if the probe brushes a ledge.
        bool nowGrounded = touching && rb.linearVelocity.y <= 0.5f;

        if (nowGrounded)
        {
            lastGroundedTime = Time.time;
            leftGroundByJump = false;
        }
        grounded = nowGrounded;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = grounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * (groundCheckRadius - 0.1f), groundCheckRadius);
    }
}
