
using UnityEngine;

public enum EAnimComponentType
{
    HitDetect,
    VFX,
    AnimParamControl,
    DisableInputInDuration
}


[System.Serializable]
public abstract class AnimStateComponent
{
    public string _debugName;
    [HideInInspector]
    public readonly EAnimComponentType componentType;

    [Range(0, 1)]
    [Header("起始时间")]
    public float normalizedStart;
    [Range(-1, 1)]
    [Header("结束时间")]
    public float normalizedEnd = -1;
    public AnimStateComponent(EAnimComponentType type)
    {
        this.componentType = type;
    }

    public virtual void ComponentEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

    }
    public virtual void ComponentUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

    }

    public virtual void ComponentExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

    }

    public bool IsExecuteOnceInState()
    {
        return normalizedEnd <= 0;
    }
}