using UnityEngine;

// On the awaken (rise from the floor) state in SummonAnimationController. The summon's AI
// waits until this state is left, so it never slides across the ground while still lying
// down. The animation is the clock - no hard-coded clip length in Summon.
public class SummonAwakenState : StateMachineBehaviour
{
    private Summon cached;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (cached == null) cached = animator.GetComponentInParent<Summon>();
        if (cached != null) cached.OnAwakened();
    }
}
