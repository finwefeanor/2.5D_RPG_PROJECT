using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Attach this to MageVisual (the child with the Animator component)
public class CharacterMotor : MonoBehaviour
{
    private Animator animator;
    private Rigidbody rb;
    private PlayerController playerController;
    private bool currentStateUsesRootMotion;

    private Vector3 accumulatedDeltaPosition;
    private Quaternion accumulatedDeltaRotation = Quaternion.identity;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponentInParent<Rigidbody>();
        playerController = GetComponentInParent<PlayerController>();
    }

    public void SetRootMotionActive(bool active)
    {
        currentStateUsesRootMotion = active;
        rb.isKinematic = active;
        if (playerController != null)
            playerController.rootMotionActive = active;

        if (!active)
        {
            accumulatedDeltaPosition = Vector3.zero;
            accumulatedDeltaRotation = Quaternion.identity;
        }
    }

    void OnAnimatorMove()
    {
        if (!currentStateUsesRootMotion) return;

        // Don't apply yet — just collect. FixedUpdate applies the total.
        accumulatedDeltaPosition += animator.deltaPosition;
        accumulatedDeltaRotation = animator.deltaRotation * accumulatedDeltaRotation;
    }

    void FixedUpdate()
    {
        if (!currentStateUsesRootMotion) return;
        if (accumulatedDeltaPosition == Vector3.zero) return;

        rb.MovePosition(rb.position + accumulatedDeltaPosition);
        rb.MoveRotation(rb.rotation * accumulatedDeltaRotation);

        accumulatedDeltaPosition = Vector3.zero;
        accumulatedDeltaRotation = Quaternion.identity;
    }
}
