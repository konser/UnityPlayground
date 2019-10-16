using UnityEngine;
using System.Collections;

public class ParamControlComponent : AnimStateComponent
{
    public string paramName;
    public int value;
    public ParamControlComponent() : base(EAnimComponentType.AnimParamControl)
    {
        _debugName = "AnimParamControl";
    }
    public override void ComponentUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"[{_debugName}] {paramName} -> {value}");
        animator.SetInteger(paramName,value);
    }
}
