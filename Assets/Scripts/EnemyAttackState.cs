using UnityEngine;

// Attach to the Attack state node in the Enemy's Animator
// (click the state box > Add Behaviour > EnemyAttackState).
// Replaces AttackStateLogger — this is the production version.
//
// The animation reports its own lifetime back to gameplay code, so the code
// never needs to know how long any clip is. Add a 3-second attack or a
// 0.4-second jab later and this keeps working with zero tuning.
public class EnemyAttackState : StateMachineBehaviour
{
    private Enemy cached;

    private Enemy Get(Animator animator)
    {
        if (cached == null) cached = animator.GetComponentInParent<Enemy>();
        return cached;
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Enemy e = Get(animator);
        if (e != null) e.OnAttackAnimationStart();
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Enemy e = Get(animator);
        if (e != null) e.OnAttackAnimationEnd();
    }
}