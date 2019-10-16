using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TransitionControlSMB : BaseStateMachineBehaviour
{
    [SerializeReference]
    public TransitionConfig config;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
    }
}
