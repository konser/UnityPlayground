using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NavManager : MonoBehaviour
{
    public static NavManager instance { get; private set; }

    private NavigationPipeline _groupPipeline;
    private NavigationPipeline _indiviualPipeline;
    #region MonoBehaviour
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Init();
    }

    private void OnDestroy()
    {
        instance = null;
    }
    #endregion

    private void Init()
    {
        _groupPipeline = new NavigationPipeline();
        _groupPipeline.AddBehaviour(new PathfindingBehaviour());
        _groupPipeline.AddBehaviour(new SlotAssignmentBehaviour());
        _groupPipeline.AddBehaviour(new FormationFollowBehaviour());
        _groupPipeline.AddBehaviour(new LocalAvoidanceBehaviour());
        _indiviualPipeline = new NavigationPipeline();
        _indiviualPipeline.AddBehaviour(new PathfindingBehaviour());
    }

    public void RequestNavigation(NavEntity entity, Vector3 targetPos)
    {
        NavigationRequest req = new NavigationRequest(entity,targetPos);
        switch (req.entity.navEntityType)
        {
            case ENavEntityType.Group:
                _groupPipeline.AddRequest(req);
                break;
            case ENavEntityType.Individual:
                _indiviualPipeline.AddRequest(req);
                break;
        }
    }

    private void Update()
    {
        // 处理通常情况下个体的寻路
        _indiviualPipeline.Update();

        // 处理群体寻路
        _groupPipeline.Update();

        HandleMovement();
    }

    #region Movement 暂时放这里

    private Dictionary<Guid,MovementRequest> _movementReqDic = new Dictionary<Guid, MovementRequest>();
    public void UpdateMovementOrder(Guid guid,MovementRequest newReq)
    {
        _movementReqDic[guid] = newReq;
    }

    public void RemoveMovementOrder(Guid guid)
    {
        _movementReqDic.Remove(guid);
    }

    private void HandleMovement()
    {
        foreach (KeyValuePair<Guid, MovementRequest> tPair in _movementReqDic)
        {
            if (tPair.Value.velocity == Vector3.zero)
            {
                continue;
            }
            NavEntity.DoMovement(tPair.Key,tPair.Value);
        }
    }
    #endregion
}
