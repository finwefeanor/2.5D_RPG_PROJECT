using UnityEngine;

// CHANGED FROM 2D:
//   Rigidbody2D  → Rigidbody
//   Vector2      → Vector3
//   rb.MovePosition(rb.position + ...) → rb.velocity (cleaner in 3D)
//   Animator removed — no sprite sheets in 3D
//   Movement is on X/Z plane (not X/Y like 2D)

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Animation (optional)")]
    public Animator animator;           // leave empty if no animator

    [HideInInspector] public bool rootMotionActive = false; // set by CharacterMotor


    private Rigidbody rb;
    private Vector3 movementDirection;

    static readonly int isMovingHash = Animator.StringToHash("isMoving");

    [SerializeField] private VirtualJoystick joystick; // assign in Inspector


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;// Lock rotation so the capsule doesn't tip over
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }


    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (joystick != null && Mathf.Abs(h) < 0.01f && Mathf.Abs(v) < 0.01f)
        {
            h = joystick.Horizontal;
            v = joystick.Vertical;
        }

        movementDirection.x = h;
        movementDirection.z = v;
        movementDirection.y = 0;

        if (!rootMotionActive && movementDirection.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(movementDirection),
                Time.deltaTime * 15f
            );
        }
        if (animator != null)
            animator.SetBool(isMovingHash, movementDirection.magnitude > 0.1f);
    }

    void FixedUpdate()
    {
        if (rootMotionActive) return; // let CharacterMotor drive the Rigidbody instead

        Vector3 velocity = movementDirection.normalized * moveSpeed;
        velocity.y = rb.velocity.y;
        rb.velocity = velocity;
    }
}
