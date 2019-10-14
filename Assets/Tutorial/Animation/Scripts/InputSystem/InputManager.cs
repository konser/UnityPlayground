using System;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private InputMapper _inputMapper;
    private Dictionary<EInputContextType,InputContext> _inputContextDic
        = new Dictionary<EInputContextType, InputContext>();
    private InputConfig _inputConfig;
    public bool showDebugInfo;
    public EInputContextType currentInputContextType;
    private HashSet<KeyCode> keycodeSet
    {
        get { return _inputMapper.keycodesToCheck; }
    }

    public static InputManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _inputMapper = new InputMapper();
        _inputConfig = Resources.Load<InputConfig>("InputConfig");
        _inputContextDic.Add(_inputConfig.normalHumanContext.inputContextType, new InputContext(_inputConfig.normalHumanContext));
        _inputContextDic.Add(_inputConfig.priorityConfig.inputContextType, new InputContext(_inputConfig.priorityConfig));
        currentInputContextType = EInputContextType.NormalHumanPlayer;
        AddInputContext(currentInputContextType);
    }

    #region API

    public void SwitchInputContext(EInputContextType contextType)
    {
        if (contextType != currentInputContextType)
        {
            RemoveInputContext(currentInputContextType);
            currentInputContextType = contextType;
            AddInputContext(contextType);
        }
    }
    private void AddInputContext(EInputContextType contextType)
    {
        _inputMapper.AddInputContext(_inputContextDic[contextType]);
    }

    private void RemoveInputContext(EInputContextType contextType)
    {
        _inputMapper.RemoveInputContext(_inputContextDic[contextType]);
    }
    #endregion

    #region 输入事件
    // Action类型输入事件
    private Dictionary<EVirtualKeyType,Action<InputData>> actionInputEventDic 
        = new Dictionary<EVirtualKeyType, Action<InputData>>();
    // State类型输入事件
    private Dictionary<EVirtualKeyType, Action<InputData>> stateInputEventDic
        = new Dictionary<EVirtualKeyType, Action<InputData>>();

    public void Register(EInputType inputType,EVirtualKeyType keyType,  Action<InputData> callback)
    {
        switch (inputType)
        {
            case EInputType.Action:
                if (actionInputEventDic.ContainsKey(keyType))
                {
                    actionInputEventDic[keyType] += callback;
                }
                else
                {
                    actionInputEventDic.Add(keyType, callback);
                }
                break;
            case EInputType.State:
                if (stateInputEventDic.ContainsKey(keyType))
                {
                    stateInputEventDic[keyType] += callback;
                }
                else
                {
                    stateInputEventDic.Add(keyType, callback);
                }
                break;
        }
    }

    public void UnRegister(EInputType inputType,EVirtualKeyType keyType, Action<InputData> callback)
    {
        switch (inputType)
        {
            case EInputType.Action:
                if (actionInputEventDic.ContainsKey(keyType))
                {
                    actionInputEventDic[keyType] -= callback;
                }
                break;
            case EInputType.State:
                if (stateInputEventDic.ContainsKey(keyType))
                {
                    stateInputEventDic[keyType] -= callback;
                }
                break;
        }
    }

    #endregion

    #region RawInput Handler

    private Dictionary<KeyCode,float> _holdedKeyThisFrame = new Dictionary<KeyCode, float>();
    private Dictionary<KeyCode,float> _firedKeyThisFrame = new Dictionary<KeyCode, float>();
    private List<KeyCode> _keyCache = new List<KeyCode>();
    private List<InputData> _thisFrameMappedInputList = new List<InputData>();
    private void BeforeReceiveInput()
    {
        // 每帧开始时清除输入
        _inputMapper.ClearLastFrameInput();
        _firedKeyThisFrame.Clear();

        // 该帧内按下的键
        _keyCache.Clear();
        foreach (KeyCode key in _holdedKeyThisFrame.Keys)
        {
            _keyCache.Add(key);
        }

        // 累计按下时间
        for (int i = 0; i < _keyCache.Count; i++)
        {
            _holdedKeyThisFrame[_keyCache[i]] += Time.deltaTime;
        }
    }

    private void ReceiveInput()
    {
        foreach (KeyCode keyCode in keycodeSet)
        {
            if (Input.GetKeyDown(keyCode))
            {
                if (_holdedKeyThisFrame.ContainsKey(keyCode) == false)
                {
                    _holdedKeyThisFrame.Add(keyCode,0f);
                }
            }
        }

        _inputMapper.RawKeyHolded(_holdedKeyThisFrame);

        foreach (KeyCode keyCode in keycodeSet)
        {
            if (Input.GetKeyUp(keyCode))
            {
                if (_holdedKeyThisFrame.ContainsKey(keyCode))
                {
                    _firedKeyThisFrame.Add(keyCode, _holdedKeyThisFrame[keyCode]);
                    _holdedKeyThisFrame.Remove(keyCode);
                }
            }
        }

        _inputMapper.RawKeyReleased(_firedKeyThisFrame);
    }

    private void AfterReceiveInput()
    {
        // 执行注册的输入回调
        _thisFrameMappedInputList = _inputMapper.GetMappedInputInThisFrame();
        for (int i = 0; i < _thisFrameMappedInputList.Count; i++)
        {
            InputData data = _thisFrameMappedInputList[i];
            switch (data.inputType)
            {
                case EInputType.Action:
                    if (actionInputEventDic.ContainsKey(data.virtualKey))
                    {
                        actionInputEventDic[data.virtualKey]?.Invoke(data);
                    }
                    break;
                case EInputType.State:
                    if (stateInputEventDic.ContainsKey(data.virtualKey))
                    {
                        stateInputEventDic[data.virtualKey]?.Invoke(data);
                    }
                    break;
            }
            //--For debug
            if (showDebugInfo) Debug.Log(data.ToString());
        }
    }
    private void Update()
    {
        BeforeReceiveInput();
        ReceiveInput();
        AfterReceiveInput();
    }
    #endregion
}
