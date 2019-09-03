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
        formationForward = forward;
    }

    public virtual void DebugDrawer()
    {

    }

    public virtual void CaculateSlotPosition()
    {

    }

    
}
