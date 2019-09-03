using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 编队模式
/// </summary>
public class FormationPattern
{
    public List<SlotInfo> slotInfoList = new List<SlotInfo>();
    public Vector3 center;
    public Vector3 formationForward;

    public void SetCenter(Vector3 c)
    {
        center = c;
    }

    public void SetForward(Vector3 forward)
    {
        // 由于朝向是根据速度得到的 速度为0时保持当前朝向不变
        if (forward.magnitude == 0)
        {
            return;
        }
        formationForward = forward;
    }

    public virtual void DebugDrawer()
    {

    }

    public virtual void CaculateSlotPosition()
    {

    }

    
}
