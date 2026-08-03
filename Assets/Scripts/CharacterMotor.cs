using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Attach this to MageVisual (the child with the Animator component)
public class CharacterMotor : MonoBehaviour
{
    private Animator animator;
    private Rigidbody rb;             // lives on the parent
    private PlayerController playerController; // optional, only exists on player
    private bool currentStateUsesRootMotion;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponentInParent<Rigidbody>();
        playerController = GetComponentInParent<PlayerController>(); // null on enemy, that's fine
    }

    public void SetRootMotionActive(bool active)
    {
        currentStateUsesRootMotion = active;
        rb.isKinematic = active; // disable physics/gravity while root motion drives position

        if (playerController != null)
            playerController.rootMotionActive = active;
    }

    void OnAnimatorMove()
    {
        if (!currentStateUsesRootMotion) return;

        Vector3 delta = animator.deltaPosition;
        rb.MovePosition(rb.position + delta);
        rb.MoveRotation(rb.rotation * animator.deltaRotation);
    }
}
