using UnityEngine;
using System.Collections;

[System.Serializable]
public class HitDetectComponent : AnimStateComponent
{
    public HitDetectComponent() : base(EAnimComponentType.HitDetect)
    {
        _debugName = "HitDetection";
    }
    public override void Execute()
    {
        Debug.Log($"{_debugName} {normalizedStart} {normalizedEnd}");
    }
}
