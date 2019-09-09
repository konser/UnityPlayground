using UnityEngine;
using System.Collections;
using System;
public class SlotInfo
{
    public Guid entityID;
    public Vector3 slotWorldPosition;
    public int entityIndex;
    public Vector3 formationCenter;
    public Vector3 offsetSlotMinusCenter;

    public SlotInfo(Vector3 center,Vector3 offset)
    {
        formationCenter = center;
        offsetSlotMinusCenter = offset;
    }

    public Vector3 GetFormationLocalPosition()
    {
        
        return slotWorldPosition - formationCenter;
    }
}