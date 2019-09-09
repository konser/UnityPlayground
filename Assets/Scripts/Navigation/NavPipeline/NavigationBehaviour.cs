using UnityEngine;
using System.Collections;

public abstract class NavigationBehaviour
{
    protected NavHandleData navHandleData;

    public virtual void ReceiveData(NavHandleData data)
    {
        navHandleData = data;
    }

    public virtual void Reset()
    {
        navHandleData = null;
    }
    public abstract void ProcessStep();

    /// <summary>
    /// todo 可能需重构
    /// 行为能否执行的特殊判定条件，例如寻路路径计算过程在一个寻路请求中只会执行一次
    /// </summary>
    public virtual bool CanExecuteProcessStep()
    {
        if (navHandleData != null)
        {
            return true;
        }
        else
        {
            Debug.LogError("NavhandleData 为空");
        }
        Debug.LogError($"执行中断 {this.GetType().Name}");
        return false;
    }
}
