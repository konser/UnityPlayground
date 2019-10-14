using System;
using System.Collections.Generic;
using UnityEngine;

public class SubStateSMB : BaseStateMachineBehaviour
{
    [SerializeReference]
    public List<AnimStateComponent> componentList  = new List<AnimStateComponent>();
    public float _stateEnterTime;
    public float _currentTime;
    public bool[] _hasExecuted;
    public bool[] _islooping;
    protected void ExecuteOnceInNormalizedTime(int index,float normalizedTime, Action action)
    {
        if (!_hasExecuted[index])
        {
            if (_currentTime >= normalizedTime)
            {
                action.Invoke();
                _hasExecuted[index] = true;
            }
        }
    }

    protected void UpdateInNormalizedRange(int index,float start, float end, Action action)
    {
        if (_currentTime >= start && _currentTime <= end)
        {
            action.Invoke();
            _islooping[index] = true;
        }
        else
        {
            _islooping[index] = false;
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
        _stateEnterTime = Time.time;
        Reset();
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateExit(animator, stateInfo, layerIndex);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        if (stateInfo.normalizedTime >= 1.0f)
        {
            return;
        }
        _currentTime = stateInfo.normalizedTime;
        for (int i = 0; i < componentList.Count; i++)
        {
            AnimStateComponent cmp = componentList[i];
            if (cmp.normalizedEnd >= 0f)
            {
                UpdateInNormalizedRange(i,cmp.normalizedStart, cmp.normalizedEnd, cmp.Execute);
            }
            else
            {
                ExecuteOnceInNormalizedTime(i,cmp.normalizedStart,cmp.Execute);
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
    #endregion
}
