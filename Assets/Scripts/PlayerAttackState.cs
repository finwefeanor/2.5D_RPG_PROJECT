using UnityEngine;

// Attach to the Attack state node in the Player's Animator Controller
// (click the state box > Add Behaviour > PlayerAttackState).
//
// The Animator lives on MagePlayerVisual while PlayerAttack.cs lives on the
// parent Player object, so this walks up to find it � same reason
// PlayerAnimationEventRelay exists.
public class PlayerAttackState : StateMachineBehaviour
{
    private PlayerAttack cached;

    private PlayerAttack Get(Animator animator)
    {
        if (cached == null) cached = animator.GetComponentInParent<PlayerAttack>();
        return cached;
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerAttack playerAttack = Get(animator);
        if (playerAttack != null) playerAttack.OnAttackAnimationStart(stateInfo.fullPathHash);
    }

    // Feeds the swing's progress (0-1) to PlayerAttack so it knows when the combo window opens.
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerAttack playerAttack = Get(animator);
        if (playerAttack != null)
            playerAttack.OnAttackAnimationUpdate(stateInfo.fullPathHash, stateInfo.normalizedTime);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerAttack playerAttack = Get(animator);
        if (playerAttack != null) playerAttack.OnAttackAnimationEnd();
    }
}