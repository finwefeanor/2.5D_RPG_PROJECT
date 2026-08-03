using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UsesRootMotion : StateMachineBehaviour
{
    public bool useRootMotion = true;


    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponent<CharacterMotor>().SetRootMotionActive(true);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponent<CharacterMotor>().SetRootMotionActive(false);
    }


}
