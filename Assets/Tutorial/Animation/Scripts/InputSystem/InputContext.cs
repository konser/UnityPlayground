using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 负责将原始输入转换为配置里的虚拟输入类型
/// </summary>
public class InputContext
{
    #region 配置项
    private Dictionary<KeyCode, InputMapRule> _actionInputMappingDic;
    private Dictionary<KeyCode, InputMapRule> _stateInputMappingDic;
    private Dictionary<KeyCode, InputMapRule> _rangeInputMappingDic;
    #endregion

    #region 运行时
    private List<InputInfo> _currentFrameInputList;
    public HashSet<KeyCode> keycodeSetToCheck;
    #endregion

    public InputContext(InputContextConfig config)
    {
        _currentFrameInputList = new List<InputInfo>();
        keycodeSetToCheck = new HashSet<KeyCode>();
        _actionInputMappingDic = new Dictionary<KeyCode, InputMapRule>();
        _stateInputMappingDic = new Dictionary<KeyCode, InputMapRule>();
        _rangeInputMappingDic = new Dictionary<KeyCode, InputMapRule>();
        for (int i = 0; i < config.inputMapRuleList.Count; i++)
        {
            EInputType inputType = config.inputMapRuleList[i].inputType;
            KeyCode keyCode = config.inputMapRuleList[i].realKeyCode;
            switch (inputType)
            {
                case EInputType.Action:
                    if (_actionInputMappingDic.ContainsKey(keyCode) == false)
                    {
                        _actionInputMappingDic.Add(keyCode,config.inputMapRuleList[i]);
                    }
                    break;
                case EInputType.State:
                    if (_stateInputMappingDic.ContainsKey(keyCode) == false)
                    {
                        _stateInputMappingDic.Add(keyCode, config.inputMapRuleList[i]);
                    }
                    break;
                case EInputType.Range:
                    if (_rangeInputMappingDic.ContainsKey(keyCode) == false)
                    {
                        _rangeInputMappingDic.Add(keyCode, config.inputMapRuleList[i]);
                    }
                    break;
            }

            keycodeSetToCheck.Add(keyCode);
        }
    }

    public bool MapInputToActionType(KeyCode keycode)
    {
        if (_actionInputMappingDic.ContainsKey(keycode))
        {
            _currentFrameInputList.Add(new InputInfo
            {
                inputType = EInputType.Action,
                virtualKey = _actionInputMappingDic[keycode].virtualKeyType,
                timeStamp = Time.time
            });
            return true;
        }

        return false;
    }

    public bool MapInputToStateType(KeyCode keycode,float holdedTime,bool isReleased)
    {
        if (_stateInputMappingDic.ContainsKey(keycode))
        {
            InputMapRule rule = _stateInputMappingDic[keycode];
            // 达到持续时间后才会计入持续输入 
            if (holdedTime < rule.enterStateTypeTime)
            {
                return false;
            }
            _currentFrameInputList.Add(new InputInfo
            {
                inputType = EInputType.State,
                virtualKey = rule.virtualKeyType,
                holdTime = holdedTime,
                isReleased = isReleased,
                timeStamp =  Time.time
            });
            return true;
        }

        return false;
    }

    public void MapInputToRangeType()
    {
        //todo
    }

    public List<InputInfo> GetCurrentFrameMappedInput()
    {
        return _currentFrameInputList;
    }

    public void ClearOldInput()
    {
        _currentFrameInputList.Clear();
    }
    public bool IsValidInput(KeyCode realKeyCode)
    {
        return false;
    }
}
