using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 给一组单位分配各自理想的位置
/// </summary>
public class SlotAssignmentBehaviour : NavigationBehaviour
{
    private NavGroup _cachedGroup;
    private List<NavHandleData> _childData;
    public override void ProcessStep()
    {
        if (navHandleData.isGroup)
        {
            _cachedGroup = (navHandleData.entity as NavGroup);
            if (_cachedGroup == null)
            {
                Debug.LogError("组对象为空！");
                return;
            }

            Vector3 currentPosition = NavEntity.GetCurrentPosition(navHandleData.entityID);
            Vector3 nextPosition = currentPosition + navHandleData.realVelocity * NavHandleData.NAV_TICK_TIME;
            _cachedGroup.AssignSlots(nextPosition, navHandleData.realVelocity.normalized);
            _childData = navHandleData.GetChildNavData();
            for (int i = 0; i < _childData.Count; i++)
            {
                _childData[i].slotPositionWhenAsChild = _cachedGroup.GetSlotInfoByEntityID(_childData[i].entityID).slotWorldPosition;
            }
            Debug.DrawLine(currentPosition,nextPosition,Color.magenta, NavHandleData.NAV_TICK_TIME);
        }
    }
}
