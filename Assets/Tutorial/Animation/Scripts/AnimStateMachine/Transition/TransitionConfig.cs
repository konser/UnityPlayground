using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TransitionConfig",menuName = "AnimConfig/Component/Transition")]
public class TransitionConfig : ScriptableObject
{
    public List<TransitionInfo> transitions;
}
