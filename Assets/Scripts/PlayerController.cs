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
    public float moveSpeed = 5f;

    private Rigidbody rb;
    private Vector3 movementDirection;
    

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation; // don't tip over
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
    }

    void FixedUpdate()
    {
        Vector3 velocity = movementDirection.normalized * moveSpeed;
        velocity.y = rb.velocity.y; // preserve gravity
        rb.velocity = velocity;
    }
}
