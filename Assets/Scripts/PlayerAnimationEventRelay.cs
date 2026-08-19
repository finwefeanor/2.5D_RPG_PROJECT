using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Attach this to MagePlayerVisual (the GameObject with the Animator),
// since Animation Events only call methods on components on that exact
// GameObject, not on parent GameObjects like "Player" where PlayerHealth.cs
// (and PlayerAttack.cs) actually live.
public class PlayerAnimationEventRelay : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private PlayerAttack playerAttack;

    void Awake()
    {
        playerHealth = GetComponentInParent<PlayerHealth>();
        playerAttack = GetComponentInParent<PlayerAttack>();

        if (playerHealth == null)
            Debug.LogWarning($"{name}: PlayerAnimationEventRelay couldn't find a PlayerHealth component in its parents.");
        if (playerAttack == null)
            Debug.LogWarning($"{name}: PlayerAnimationEventRelay couldn't find a PlayerAttack component in its parents.");
    }

    // Wire the Hit clip's Animation Event to THIS method name.
    public void PlayHitSound()
    {
        if (playerHealth != null) playerHealth.PlayHitSound();
    }

    // Bonus: if you later add a PlayAttackSound() method to PlayerAttack.cs
    // (as discussed), this is ready to relay that too — same pattern.
    public void PlayAttackSound()
    {
        if (playerAttack != null) playerAttack.PlayAttackSound();
    }
}
