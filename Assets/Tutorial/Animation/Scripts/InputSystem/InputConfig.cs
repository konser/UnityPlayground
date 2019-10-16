using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InputMapRule
{
    public EInputType inputType;
    public KeyCode realKeyCode;
    public EVirtualKeyType virtualKeyType;
    public float enterStateTypeTime = 0.1f;
}

[System.Serializable]
public class InputContextConfig
{
    public EInputContextType inputContextType;
    public List<InputMapRule> inputMapRuleList;
}

[CreateAssetMenu(fileName = "InputConfig")]
public class InputConfig : ScriptableObject
{
    public InputContextConfig priorityConfig;
    public InputContextConfig normalHumanContext;
}