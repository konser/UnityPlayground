using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ENavEntityType
{
    Individual,
    Group
}

public enum ENavReqState
{
    WaitForProcess,
    IsProcessing,
    Interrupted,
    IsDone
}

/// <summary>
/// 寻路请求
/// </summary>
public class NavigationRequest
{
    public readonly NavEntity entity;

    public Vector3 destination;

    public ENavReqState state;

    /// <summary>
    /// 寻路请求标识ID 等于请求寻路的对象的ID
    /// </summary>
    public readonly System.Guid reqID;
    public NavigationRequest(NavEntity e, Vector3 dest)
    {
        entity = e;
        destination = dest;
        state = ENavReqState.WaitForProcess;
        reqID = entity.entityID;
    }
}

/// <summary>
/// 移动请求
/// </summary>
public struct MovementRequest
{
    public System.Guid entityID;
    public Vector3 velocity;
}