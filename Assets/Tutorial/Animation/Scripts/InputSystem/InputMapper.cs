﻿using System.Collections.Generic;
using UnityEngine;

public class InputMapper
{
    private List<KeyCode> _consumeHoldedKeycodeList = new List<KeyCode>();
    private List<KeyCode> _consumeReleaseKeyCodeList = new List<KeyCode>();
    public List<InputContext> inputContextList = new List<InputContext>();
    public HashSet<KeyCode> keycodesToCheck = new HashSet<KeyCode>();
    private List<InputInfo> _allInputThisFrame = new List<InputInfo>();
    public void AddInputContext(InputContext inputContext)
    {
        if (inputContextList.Contains(inputContext) == false)
        {
            inputContextList.Add(inputContext);
        }
        UpdateKeyCodesNeedToCheck();
    }

    public void RemoveInputContext(InputContext inputContext)
    {
        if (inputContextList.Contains(inputContext))
        {
            inputContextList.Remove(inputContext);
        }
        UpdateKeyCodesNeedToCheck();
    }

    public void ClearLastFrameInput()
    {
        for (int i = 0; i < inputContextList.Count; i++)
        {
            inputContextList[i].ClearOldInput();
        }
    }

    /// <summary>
    /// 接收释放的实际按键，转换为游戏逻辑的按键
    /// </summary>
    public void RawKeyReleased(Dictionary<KeyCode,float> firedKeyThisFrame)
    {
        _consumeReleaseKeyCodeList.Clear();
        for (int i = inputContextList.Count - 1; i >= 0; i--)
        {
            foreach (KeyValuePair<KeyCode, float> pair in firedKeyThisFrame)
            {
                // 该按键被其他输入环境处理过了，则跳过
                if (_consumeReleaseKeyCodeList.Contains(pair.Key))
                {
                    continue;
                }

                if (pair.Value > InputConstant.MIN_HOLD_TIME 
                    && inputContextList[i].MapInputToStateType(pair.Key, pair.Value, true))
                {
                    _consumeReleaseKeyCodeList.Add(pair.Key);
                    continue;
                }

                if (inputContextList[i].MapInputToActionType(pair.Key))
                {
                    _consumeReleaseKeyCodeList.Add(pair.Key);
                }
            }
        }
    }
    /// <summary>
    /// 接收按下状态的实际按键，转换为游戏逻辑的按键
    /// </summary>
    public void RawKeyHolded(Dictionary<KeyCode, float> holdedKeyDic)
    {
        _consumeHoldedKeycodeList.Clear();
        for (int i = inputContextList.Count-1; i >= 0; i--)
        {
            foreach (KeyValuePair<KeyCode, float> pair in holdedKeyDic)
            {        
                // 该按键被其他输入环境处理过了，则跳过
                if (_consumeHoldedKeycodeList.Contains(pair.Key))
                {
                    continue;
                }

                // 按键未被释放时的更新
                if (inputContextList[i].MapInputToStateType(pair.Key, pair.Value,false))
                {
                    _consumeHoldedKeycodeList.Add(pair.Key);
                }
            }
        }
    }

    public List<InputInfo> GetMappedInputInThisFrame()
    {
        _allInputThisFrame.Clear();
        for (int i = 0; i < inputContextList.Count; i++)
        {
            _allInputThisFrame.AddRange(inputContextList[i].GetCurrentFrameMappedInput());
        }

        return _allInputThisFrame;
    }

    public void HandleAxiRawInput(string axiName)
    {

    }

    private void UpdateKeyCodesNeedToCheck()
    { 
        keycodesToCheck.Clear();
        foreach (InputContext context in inputContextList)
        {
            keycodesToCheck.UnionWith(context.keycodeSetToCheck);
        }
    }
}
