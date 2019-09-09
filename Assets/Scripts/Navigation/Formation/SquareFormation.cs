using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SquareFormation : FormationPattern
{
    public int rowCount = 4;
    public int agentCount = 16;
    public List<int> eachRowCountList = new List<int>();
    public float radius = 2f;
    public  void InitFormation(Vector3 center,Vector3 forward,int agentCount,int rowCount,float radius = 2f)
    {
        this.center = center;
        this.formationForward = forward.normalized;
        this.rowCount = rowCount;
        this.agentCount = agentCount;
        this.radius = radius;
        int eachRowAgentNum = Mathf.CeilToInt(((float)agentCount / (float)rowCount));
        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, formationForward);
        int fullCount = agentCount / eachRowAgentNum;
        int remain = agentCount % eachRowAgentNum;
        for (int i = 0; i < fullCount; i++)
        {
            eachRowCountList.Add(eachRowAgentNum);
        }
        if (remain != 0)
        {
            eachRowCountList.Add(remain);
        }
        
        float halfVLength = radius  * (eachRowCountList.Count - 1);
        float halfHLength = radius * (eachRowAgentNum - 1);
        for (int i = 0; i < eachRowCountList.Count; i++)
        {
            for (int j = 0; j < eachRowCountList[i]; j++)
            {
                Vector3 offset = new Vector3(-halfHLength + 2*j*radius,0, halfVLength - 2*i*radius);
                SlotInfo info = new SlotInfo(center, offset);
                slotInfoList.Add(info);
                info.slotWorldPosition = center + rot * info.offsetSlotMinusCenter;
            }
        }
    }

    public override void CaculateSlotPosition()
    {
        for (int i = 0; i < slotInfoList.Count; i++)
        {
            Quaternion rot = Quaternion.FromToRotation(Vector3.forward, formationForward);
            slotInfoList[i].slotWorldPosition = center + rot * slotInfoList[i].offsetSlotMinusCenter;
        }
    }

    public override void DebugDrawer()
    {
        Utility.DrawBlueUpLine(center,Time.deltaTime);
        for (int i = 0; i < slotInfoList.Count; i++)
        {
            Utility.DrawGreenUpLine(slotInfoList[i].slotWorldPosition,Time.deltaTime);
        }
    }
}
