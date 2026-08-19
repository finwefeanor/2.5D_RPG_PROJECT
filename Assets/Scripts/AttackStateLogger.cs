using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Attach this directly to the Attack state node in the Animator window
// (click the Attack state box, Inspector > Add Behaviour > search this name).
// Gives exact, unambiguous timestamps for when the Animator itself actually
// enters/exits the state — no more relying on eyeballing swings or inferring
// from code-side Debug.logs, which only prove when a trigger was CALLED,
// not when the animation actually played.
public class AttackStateLogger : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"[{animator.gameObject.name}] ENTERED Attack state at {Time.time:F2}s");
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"[{animator.gameObject.name}] EXITED Attack state at {Time.time:F2}s");
    }
}
