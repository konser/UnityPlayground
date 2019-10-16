using System;
using System.CodeDom;
using System.Collections.Generic;
using UnityEngine;

public class SubStateSMB : BaseStateMachineBehaviour
{
    [SerializeReference]
    [Header("组件列表")]
    public List<AnimStateComponent> componentList  = new List<AnimStateComponent>();
    [Header("调试信息")]
    public float _stateEnterTime;
    public float _currentTime;
    public bool[] _hasExecuted;
    public bool[] _islooping;
    protected void ExecuteOnceInNormalizedTime(int index,Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        AnimStateComponent component = componentList[index];
        if (!_hasExecuted[index])
        {
            if (_currentTime >= component.normalizedStart)
            {
                component.ComponentEnter(animator,stateInfo,layerIndex);
                component.ComponentUpdate(animator, stateInfo, layerIndex);
                component.ComponentExit(animator, stateInfo, layerIndex);
                _hasExecuted[index] = true;
            }
        }
    }

    protected void UpdateInNormalizedRange(int index,Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        AnimStateComponent component = componentList[index];
        if (!_islooping[index] && _currentTime >= component.normalizedStart && _currentTime <= component.normalizedEnd)
        {
            component.ComponentEnter(animator, stateInfo, layerIndex);
            _islooping[index] = true;
        }
        else if (_islooping[index] && _currentTime >= component.normalizedEnd)
        {
            component.ComponentExit(animator, stateInfo, layerIndex);
            _islooping[index] = false;
        }
        else if(_currentTime >= component.normalizedStart && _currentTime <= component.normalizedEnd)
        {
           component.ComponentUpdate(animator,stateInfo,layerIndex);
        }
    }


    protected void Reset()
    {
        _hasExecuted = new bool[componentList.Count];
        _islooping = new bool[componentList.Count];
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        Reset();
        _stateEnterTime = Time.time;
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateExit(animator, stateInfo, layerIndex);
        // 如果状态退出时有触发一次的组件没执行，需要执行一次，因为按NormalizedTime判定不准确
        for (int i = 0; i < componentList.Count; i++)
        {
            if (componentList[i].IsExecuteOnceInState() && !_hasExecuted[i])
            {
                componentList[i].ComponentUpdate(animator,stateInfo,layerIndex);
            }
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        if (stateInfo.normalizedTime > 1.0f)
        {
            return;
        }
        _currentTime = stateInfo.normalizedTime;
        for (int i = 0; i < componentList.Count; i++)
        {
            AnimStateComponent cmp = componentList[i];
            if (!cmp.IsExecuteOnceInState())
            {
                UpdateInNormalizedRange(i,animator,stateInfo,layerIndex);
            }
            else
            {
                ExecuteOnceInNormalizedTime(i, animator, stateInfo, layerIndex);
            }
        }
    }

    #region AddComponent
    [ContextMenu("HitDetection/Base")]
    public void AddHitDetectComponent()
    {
        HitDetectComponent cmp = new HitDetectComponent();
        componentList.Add(cmp);
    }

    [ContextMenu("VFX/Base")]
    public void AddVFXComponent()
    {
        VFXComponent cmp = new VFXComponent();
        componentList.Add(cmp);
    }

    [ContextMenu("ParamControl/Int")]
    public void AddParamControl()
    {
        ParamControlComponent cmp = new ParamControlComponent();
        componentList.Add(cmp);
    }

    [ContextMenu("Miscellaneous/DisableInputInDuration")]
    public void AddDisableInputInDuration()
    {
        DisableInputDuration cmp = new DisableInputDuration();
        componentList.Add(cmp);
    }
    #endregion
}
