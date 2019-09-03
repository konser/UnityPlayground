using System.Collections.Generic;
using RVO;
using UnityEngine;

public class FormationFollowBehaviour : NavigationBehaviour
{
    private List<NavHandleData> _childData;
    private SlotInfo _slotInfo;
    public override void ProcessStep()
    {
        _childData = navHandleData.GetChildNavData();
        if (_childData != null)
        {
            NavGroup group = (NavGroup)navHandleData.entity;
            for (int i = 0; i < _childData.Count; i++)
            {
                _slotInfo = group.GetSlotInfoByEntityID(_childData[i].entityID);
                if (_slotInfo == null)
                {
                    continue;
                }

                float speed = _childData[i].entity.maxSpeed;
                float distance = Vector3.Distance(NavEntity.GetCurrentPosition(_childData[i].entityID), _slotInfo.slotWorldPosition) *
                                 (1f / NavigationPipeline.NAV_TICK_TIME);
                // 例如 一秒内距离小于3 速度为3 则速度为初始最大不变
                // 若距离大于3 例如6 则为两倍速
                if (distance > speed)
                {
                    speed = (distance / speed) * speed;
                }

                _childData[i].realVelocity = (_slotInfo.slotWorldPosition - NavEntity.GetCurrentPosition(_childData[i].entityID)).normalized * speed;
                Simulator.Instance.setAgentPrefVelocity(i,_childData[i].realVelocity.ToRVOVec2());
            }
        }
    }
}
