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
        InitVirtualKeyAccessableDic();
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

    public void DisableVirtualKey(string virtualKey)
    {
        _keyAccessableNameDic[virtualKey] = false;
    }

    public void EnableVirutalKey(string virtualKey)
    {
        _keyAccessableNameDic[virtualKey] = true;
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
    /// <summary>
    /// 当前逻辑按键的禁用状态，如果禁用（false) 则不在AfterReceiveInput阶段触发按键回调
    /// todo 因为StateMachineBehaviour的Editor面板不显示枚举，只能用字符串进行配置 这里用字符串来比较
    /// </summary>
    private Dictionary<string, bool> _keyAccessableNameDic = new Dictionary<string, bool>();
    private Dictionary<EVirtualKeyType, string> _keyEnumToNameDic = new Dictionary<EVirtualKeyType, string>();

    private void Update()
    {
        BeforeReceiveInput();
        ReceiveInput();
        AfterReceiveInput();
    }

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
            // 将释放的按键加入释放按键列表，由InputMapper转换为当前输入环境的逻辑按键
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
            if (!IsKeyAccessable(data.virtualKey))
            {
                Debug.Log($"{data.virtualKey} 当前被禁用！");
                continue;
            }
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

    private void InitVirtualKeyAccessableDic()
    {
        Array keyEnums = Enum.GetValues(typeof(EVirtualKeyType));
        foreach (object tKeyEnum in keyEnums)
        {
            EVirtualKeyType key = (EVirtualKeyType)tKeyEnum;
            _keyAccessableNameDic.Add(key.ToString(), true);
            _keyEnumToNameDic.Add(key, key.ToString());
        }
    }

    private bool IsKeyAccessable(EVirtualKeyType key)
    {
        return _keyAccessableNameDic[_keyEnumToNameDic[key]];
    }

    #endregion
}
