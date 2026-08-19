using UnityEngine;

// Attach to MagePlayerVisual (the GameObject with the Animator).
// Animation Events only reach components on the exact GameObject playing the
// clip, so this forwards them up to PlayerHealth / PlayerAttack on the parent.
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

    // Hit_A clip, flinch frame (shared clip - the enemy relay has this too).
    public void PlayHitSound()
    {
        if (playerHealth != null) playerHealth.PlayHitSound();
    }

    // Attack clip, contact frame 8.
    public void PlayAttackSound()
    {
        if (playerAttack != null) playerAttack.PlayAttackSound();
    }

    // Attack clip, contact frame 8 - the swing itself decides when it lands.
    public void DealAttackDamage()
    {
        if (playerAttack != null) playerAttack.DealAttackDamage();
    }
}