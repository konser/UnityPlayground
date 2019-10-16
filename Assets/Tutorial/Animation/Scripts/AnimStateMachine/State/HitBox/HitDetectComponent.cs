using UnityEngine;
using System.Collections;

[System.Serializable]
public class HitDetectComponent : AnimStateComponent
{
    public HitDetectComponent() : base(EAnimComponentType.HitDetect)
    {
        _debugName = "HitDetection";
    }
    public override void ComponentUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"{_debugName} {normalizedStart} {normalizedEnd}");
    }
}
