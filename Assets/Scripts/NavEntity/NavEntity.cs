using System;
using System.Collections.Generic;
using UnityEngine;

public interface INavAgent
{
    Vector3 GetCurrentPosition();
    Vector3 GetForward();
    void AgentMove(MovementRequest movementReq);

    void InitAgent(Vector3 pos);
}

public abstract class NavEntity
{
    public System.Guid entityID;
    public abstract ENavEntityType navEntityType { get; }

    public INavAgent controlledAgent;

    public readonly float maxSpeed = NavHandleData.ENTITY_MAX_SPEED;

    public NavEntity(INavAgent controlledTarget)
    {
        entityID = System.Guid.NewGuid();
        _entityDic[entityID] = this;
        controlledAgent = controlledTarget;
    }

    #region Static helper
    private static Dictionary<Guid,NavEntity> _entityDic = new Dictionary<Guid, NavEntity>();
    public static Vector3 GetCurrentPosition(Guid id)
    {
        if (_entityDic.ContainsKey(id))
        {
            return _entityDic[id].controlledAgent.GetCurrentPosition();
        }
        return Vector3.negativeInfinity;
    }

    public static Vector3 GetForward(Guid id)
    {
        if (_entityDic.ContainsKey(id))
        {
            return _entityDic[id].controlledAgent.GetForward();
        }
        return Vector3.negativeInfinity;
    }

    public static float GetMaxSpeed(Guid id)
    {
        if (_entityDic.ContainsKey(id))
        {
            return _entityDic[id].maxSpeed;
        }
        return 0;
    }

    public static void InitAgent(Guid id, Vector3 pos)
    {
        if (_entityDic.ContainsKey(id))
        {
            _entityDic[id].controlledAgent.InitAgent(pos);
        }
    }

    public static void DoMovement(Guid id,MovementRequest req)
    {
        if (_entityDic.ContainsKey(id))
        {
            _entityDic[id].controlledAgent.AgentMove(req);
        }
    }
    #endregion
}
