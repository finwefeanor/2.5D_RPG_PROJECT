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

    }

    void OnAnimatorMove()
    {
        if (!currentStateUsesRootMotion) return;

        rb.MovePosition(rb.position + animator.deltaPosition);
        rb.MoveRotation(animator.deltaRotation * rb.rotation);

    }


}
