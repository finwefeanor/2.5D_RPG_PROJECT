using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Attach this to the SAME GameObject as the Animator (e.g. "Skeleton_Minion"),
// since Animation Events only call methods on components on that exact GameObject,
// not on parent GameObjects like "Enemy_0_with_sword" where Enemy.cs actually lives.
public class AnimationEventRelay : MonoBehaviour
{

    private ICombatAnimationReceiver receiver;
    private Enemy enemy;

    void Awake()
    {
        receiver = GetComponentInParent<ICombatAnimationReceiver>();
        if (receiver == null)
            Debug.LogWarning($"{name}: AnimationEventRelay found no Enemy / Summon in its parents.");
    }

    // Wire the Animation Event's Function field to THIS method name.
    public void PlayAttackSound()
    {
        if (receiver != null) receiver.PlayAttackSound();
    }

    // Also relays the shared Hit_A clip's event (same clip the Player uses).
    public void PlayHitSound()
    {
        if (receiver != null) receiver.PlayHitSound();
    }

    public void DealAttackDamage()
    {
        if (receiver != null) receiver.DealAttackDamage();
    }

}
