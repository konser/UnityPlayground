
using UnityEngine;

public enum EAnimComponentType
{
    HitDetect,
    VFX
}


[System.Serializable]
public abstract class AnimStateComponent
{
    public string _debugName;
    [HideInInspector]
    public readonly EAnimComponentType componentType;

    [Range(0,1)]
    public float normalizedStart;
    [Range(-1,1)]
    public float normalizedEnd = -1;
    public AnimStateComponent(EAnimComponentType type)
    {
        this.componentType = type;
    }

    public abstract void Execute();


}