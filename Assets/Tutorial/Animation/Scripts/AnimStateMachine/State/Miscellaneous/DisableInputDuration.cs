using UnityEngine;
using System.Collections;

public class DisableInputDuration : AnimStateComponent
{
    public string keyType;
    private float _time;
    public DisableInputDuration() : base(EAnimComponentType.DisableInputInDuration)
    {
        _debugName = "DisableInputDuration";
    }

    public override void ComponentEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.ComponentEnter(animator, stateInfo, layerIndex);
        InputManager.Instance.DisableVirtualKey(keyType);
    }

    public override void ComponentUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        InputManager.Instance.DisableVirtualKey(keyType);
    }

    public override void ComponentExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.ComponentExit(animator, stateInfo, layerIndex);
        InputManager.Instance.EnableVirutalKey(keyType);
    }
}
