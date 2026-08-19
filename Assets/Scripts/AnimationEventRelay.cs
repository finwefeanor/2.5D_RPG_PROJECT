using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Attach this to the SAME GameObject as the Animator (e.g. "Skeleton_Minion"),
// since Animation Events only call methods on components on that exact GameObject,
// not on parent GameObjects like "Enemy_0_with_sword" where Enemy.cs actually lives.
public class AnimationEventRelay : MonoBehaviour
{
    private Enemy enemy;

    void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
        if (enemy == null)
            Debug.LogWarning($"{name}: AnimationEventRelay couldn't find an Enemy component in its parents.");
    }

    // Wire the Animation Event's Function field to THIS method name.
    public void PlayAttackSound()
    {
        if (enemy != null) enemy.PlayAttackSound();
    }

    // Also relays the shared Hit_A clip's event (same clip the Player uses).
    public void PlayHitSound()
    {
        if (enemy != null) enemy.PlayHitSound();
    }

    public void DealAttackDamage()
    {
        if (enemy != null) enemy.DealAttackDamage();
    }

}
