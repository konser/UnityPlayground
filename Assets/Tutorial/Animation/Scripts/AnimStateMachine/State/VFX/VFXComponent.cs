using UnityEngine;
using System.Collections;

[System.Serializable]
public class VFXComponent : AnimStateComponent
{
    public VFXComponent() : base(EAnimComponentType.VFX)
    {
        _debugName = "VFX";
    }
    public override void Execute()
    {
        Debug.Log($"{_debugName} {normalizedStart} ");
    }
}