using System;
using System.Collections.Generic;
using UnityEngine;
public class NavGroup : NavEntity
{
    /*
     * 为方便处理，组对象不能再包含组对象
     * 如果需要逻辑上再给组对象分组，不应该再由寻路系统处理
     * 例如将几组兵框在一起朝一个目标前进，对寻路系统来说每组兵都是单独处理的（可能有一些碰撞数据的共享）
     */

    /// <summary>
    /// 组包含的寻路个体
    /// </summary>
    public List<NavIndividual> individualList = new List<NavIndividual>();
    public override ENavEntityType navEntityType
    {
        get { return ENavEntityType.Group; }
    }

    public NavGroup(INavAgent controlledTarget) : base(controlledTarget)
    {

    }

    #region 编队相关

    public bool hasFormation
    {
        get { return _formationPattern != null; }   
    }
    private FormationPattern _formationPattern;
    private Dictionary<Guid, SlotInfo> _lookUpTable = new Dictionary<Guid, SlotInfo>();
    public void AssignFormation(FormationPattern formationPattern)
    {
        _formationPattern = formationPattern;
        AssignSlots(this.controlledAgent.GetCurrentPosition(),this.controlledAgent.GetForward());
        for (int i = 0; i < _formationPattern.slotInfoList.Count; i++)
        {
            NavEntity.InitAgent(_formationPattern.slotInfoList[i].entityID,_formationPattern.slotInfoList[i].slotWorldPosition);
        }
    }

    public void AssignSlots(Vector3 pos, Vector3 forward)
    {
        // 设置中心位置
        _formationPattern.SetCenter(pos);
        _formationPattern.SetForward(forward);
        _formationPattern.CaculateSlotPosition();
        for (int i = 0; i < individualList.Count; i++)
        {
            _formationPattern.slotInfoList[i].entityID = individualList[i].entityID;
            _lookUpTable[individualList[i].entityID] = _formationPattern.slotInfoList[i];
        }
    }

    public SlotInfo GetSlotInfoByEntityID(Guid id)
    {
        return _lookUpTable[id];
        //return _formationPattern.slotInfoList.Find(info => info.entityID == id);
    }

    public void DebugDraw()
    {
        _formationPattern.DebugDrawer();
    }

    #endregion
}
