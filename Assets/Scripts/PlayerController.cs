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

    private Rigidbody rb;
    private Vector3 movementDirection;

    static readonly int isMovingHash = Animator.StringToHash("isMoving");
    

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;// Lock rotation so the capsule doesn't tip over
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        // Input is the same — only axis mapping changes (Y axis → Z axis in 3D)
        movementDirection.x = Input.GetAxisRaw("Horizontal");
        movementDirection.z = Input.GetAxisRaw("Vertical");
        movementDirection.y = 0;

        // Rotate character to face movement direction
        if (movementDirection.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(movementDirection),
                Time.deltaTime * 15f
            );
        }

        // Animator integration
        if (animator != null)
            animator.SetBool(isMovingHash, movementDirection.magnitude > 0.1f);
    }

    void FixedUpdate()
    {
        Vector3 velocity = movementDirection.normalized * moveSpeed;
        velocity.y = rb.velocity.y; // preserve gravity
        rb.velocity = velocity;
    }
}
