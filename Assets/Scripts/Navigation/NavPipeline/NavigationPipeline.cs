using System;
using System.Collections.Generic;
using System.Linq;
using RVO;
using UnityEngine;
using Vector2 = RVO.Vector2;

public class NavigationPipeline
{
    private Queue<NavigationRequest> _navReqQueue;
    private Dictionary<Guid, NavHandleData> _processingData;
    private List<NavigationBehaviour> _navigationBehaviourList;
    private List<Guid> _loopProcessingKeyList;
    private NavHandleData _loopNavHandleData;

    #region RVO 
    public List<List<RVO.Vector2>> obstacles = new List<List<Vector2>>();
    private void GetRVOObstacles()
    {
        var gameObjects =  GameObject.FindGameObjectsWithTag("RVOObstacle");
        for (int i = 0; i < gameObjects.Length; i++)
        {
            Renderer render = gameObjects[i].GetComponent<Renderer>();
            if (render == null)
            {
                continue;
            }
            Vector3 p1 = render.bounds.min;
            Vector3 p7 = render.bounds.max;
            float dx = p7.x - p1.x;
            float dy = p7.y - p1.y;
            float dz = p7.z - p1.z;
            Vector3 p2 = p1 + new Vector3(dx, 0, 0);
            Vector3 p3 = p1 + new Vector3(dx, 0, dz);
            Vector3 p4 = p1 + new Vector3(0, 0, dz);
            obstacles.Add(new List<Vector2>());
            obstacles[obstacles.Count - 1].Add(new RVO.Vector2(p1.x, p1.z));
            obstacles[obstacles.Count - 1].Add(new RVO.Vector2(p2.x, p2.z));
            obstacles[obstacles.Count - 1].Add(new RVO.Vector2(p3.x, p3.z));
            obstacles[obstacles.Count - 1].Add(new RVO.Vector2(p4.x, p4.z));
            Simulator.Instance.addObstacle(obstacles[obstacles.Count - 1]);
        }
        Simulator.Instance.processObstacles();
    }

    #endregion

    public NavigationPipeline()
    {
        _navReqQueue = new Queue<NavigationRequest>(10);
        _processingData = new Dictionary<Guid, NavHandleData>();
        _navigationBehaviourList = new List<NavigationBehaviour>();
        Simulator.Instance.setTimeStep(NavHandleData.NAV_TICK_TIME);
        GetRVOObstacles();
    }

    public void AddRequest(NavigationRequest req)
    {
        _navReqQueue.Enqueue(req);
    }

    public void AddBehaviour(NavigationBehaviour behaviour)
    {
        _navigationBehaviourList.Add(behaviour);
    }

    public void Update()
    {
        int handleCount = 0;
        while (_navReqQueue.Count != 0 && handleCount < NavHandleData.MAX_REQ_COUNT_PRE_FRAME)
        {
            handleCount++;
            NavigationRequest req = _navReqQueue.Dequeue();
            // 如果该请求与之前正在处理的ID相同，则会覆盖之前的
            _processingData[req.reqID] = new NavHandleData(req,req.entity);
            _processingData[req.reqID].lastTickTime = Time.time;
        }

        // 遍历所有正在处理的请求 进行寻路处理
        _loopProcessingKeyList = _processingData.Keys.ToList();
        foreach (Guid id in _loopProcessingKeyList)
        {
            _loopNavHandleData = _processingData[id];
            if (Time.time - _loopNavHandleData.lastTickTime > NavHandleData.NAV_TICK_TIME)
            {
                _loopNavHandleData.lastTickTime = Time.time;
                Process(_loopNavHandleData);
            }
        }

    }

    private List<NavHandleData> _cacheChildData;
    private void Process(NavHandleData data)
    {
        for (int i = 0; i < _navigationBehaviourList.Count; i++)
        {
            _navigationBehaviourList[i].ReceiveData(data);

            if (_navigationBehaviourList[i].CanExecuteProcessStep())
            {
                _navigationBehaviourList[i].ProcessStep();
            }
            _navigationBehaviourList[i].Reset();
        }

        RaiseMovementRequest(data);
    }

    private void RaiseMovementRequest(NavHandleData data)
    {
        _cacheChildData = data.GetChildNavData();
        if (data.HasReachedTarget())
        {
            NavManager.instance.RemoveMovementOrder(data.entityID);
            if (_cacheChildData != null)
            {
                foreach (NavHandleData tData in _cacheChildData)
                {
                    NavManager.instance.RemoveMovementOrder(tData.entityID);
                }
            }
            // todo  清除RVO模拟
            Simulator.Instance.Clear();
            Debug.Log($"寻路请求{data.sourceRequest.reqID}完成 所有单位到达目标");
            // 从正在处理的寻路列表移除
            _processingData.Remove(data.sourceRequest.reqID);
        }
        else
        {
            NavManager.instance.UpdateMovementOrder(data.entityID, data.ConvertToMovementRequest());
            if (_cacheChildData != null)
            {
                foreach (NavHandleData tData in _cacheChildData)
                {
                    NavManager.instance.UpdateMovementOrder(tData.entityID, tData.ConvertToMovementRequest());
                }
            }
        }
    }
}