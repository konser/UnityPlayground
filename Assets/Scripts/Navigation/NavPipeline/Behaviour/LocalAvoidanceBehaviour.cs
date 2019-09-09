using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RVO;

public class LocalAvoidanceBehaviour : NavigationBehaviour
{
    private List<NavHandleData> _childData;
    public override void ProcessStep()
    {
        Simulator.Instance.doStep();
        _childData = navHandleData.GetChildNavData();
        if (_childData != null)
        {
            for (int i = 0; i < _childData.Count; i++)
            {
                _childData[i].realVelocity = Simulator.Instance.getAgentVelocity(i).RVOToVec3();
            }
        }
    }
}
