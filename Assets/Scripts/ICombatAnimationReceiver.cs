using UnityEngine;

// Implemented by every character that runs EnemyAnimationController (Enemy, Summon).
// AnimationEventRelay and EnemyAttackState talk to this instead of to Enemy directly,
// so one relay, one state behaviour and one controller serve both.
public interface ICombatAnimationReceiver
{
    void OnAttackAnimationStart();
    void OnAttackAnimationEnd();
    void DealAttackDamage();
    void PlayAttackSound();
    void PlayHitSound();

}
