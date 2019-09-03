using RVO;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Vector2 = RVO.Vector2;

public class RVOTest : MonoBehaviour
{
    public float circleRadius = 200f;
    public int agentCount = 1000;
    List<RVO.Vector2> goals;
    private List<GameObject> agents;
    private List<List<Vector2>> obstacles;
    public float neibourDist = 8f;
    public int maxNeibours = 20;
    public float timeHorizon = 10f;
    public float obstacleTimeHorizon = 10f;
    public float radius = 1.5f;
    public float maxSpeed = 3f;
    private List<Vector2> dirs;
    private void ConvertToObstacle()
    {
        var renders = this.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renders.Length; i++)
        {
            Vector3 p1 = renders[i].bounds.min;
            Vector3 p7 = renders[i].bounds.max;
            float dx = p7.x - p1.x;
            float dy = p7.y - p1.y;
            float dz = p7.z - p1.z;
            Vector3 p2 = p1 + new Vector3(dx, 0, 0);
            Vector3 p3 = p1 + new Vector3(dx, 0, dz);
            Vector3 p4 = p1 + new Vector3(0, 0, dz);
            obstacles.Add(new List<Vector2>());
            obstacles[obstacles.Count - 1].Add(new RVO.Vector2(p1.x,p1.z));
            obstacles[obstacles.Count - 1].Add(new RVO.Vector2(p2.x, p2.z));
            obstacles[obstacles.Count - 1].Add(new RVO.Vector2(p3.x, p3.z));
            obstacles[obstacles.Count - 1].Add(new RVO.Vector2(p4.x, p4.z));
            Simulator.Instance.addObstacle(obstacles[obstacles.Count - 1]);
        }
        Simulator.Instance.processObstacles();
    }

    private void Start()
    {
        goals = new List<Vector2>();
        agents = new List<GameObject>();
        obstacles = new List<List<Vector2>>();
        dirs = new List<Vector2>();
        Init();
    }

    private bool inited;
    public void Init()
    {
        Simulator.Instance.setTimeStep(0.15f);
        Simulator.Instance.setAgentDefaults(neibourDist,maxNeibours,timeHorizon,obstacleTimeHorizon,radius,maxSpeed,new RVO.Vector2(0.0f,0.0f));
        for (int i = 0; i < agentCount; i++)
        {
            RVO.Vector2 pos = circleRadius *
                              new RVO.Vector2((float)Math.Cos(i * 2.0f * Math.PI / agentCount),
                                  (float)Math.Sin(i * 2.0f * Math.PI / agentCount));
            Simulator.Instance.addAgent(pos);
            dirs.Add(RVOMath.normalize(-Simulator.Instance.getAgentPosition(i) - Simulator.Instance.getAgentPosition(i)));
            goals.Add(-Simulator.Instance.getAgentPosition(i));

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = new Vector3(pos.x(),120,pos.y());
            agents.Add(go);
        }
        ConvertToObstacle();
        inited = true;

    }
    void SetPreferredVelocities()
    {
        /*
         * Set the preferred velocity to be a vector of unit magnitude
         * (speed) in the direction of the goal.
         */
        Vector3  g = Vector3.zero;
        for (int i = 0; i < Simulator.Instance.getNumAgents(); ++i)
        {
            RVO.Vector2 goalVector = goals[i] - Simulator.Instance.getAgentPosition(i);

            if (RVOMath.absSq(goalVector) > 1.0f)
            {
                goalVector = RVOMath.normalize(goalVector);
            }
            Simulator.Instance.setAgentPrefVelocity(i, goalVector);
        }
    }

    bool ReachedGoal()
    {
        /* Check if all agents have reached their goals. */
        for (int i = 0; i < Simulator.Instance.getNumAgents(); ++i)
        {
            if (RVOMath.absSq(Simulator.Instance.getAgentPosition(i) - goals[i]) > Simulator.Instance.getAgentRadius(i) * Simulator.Instance.getAgentRadius(i))
            {
                return false;
            }
        }

        return true;
    }


    private void Draw()
    {
        if (inited == false || Application.isPlaying == false)
        {
            return;
        }
        Handles.color = Color.red;
        for (int i = 0; i < obstacles.Count; i++)
        {
            Handles.DrawPolyLine(ToVec3GizmoArray(obstacles[i]));
        }
    }

    void OnDrawGizmos()
    {
        Draw();
    }

    private RVO.Vector2 vec2;
    private Vector3 realPos;
    private Vector3 velocity;
    private Vector3 agentPos;
    private RVO.Vector2 zeroVec = new Vector2(0,0);
    private float lastTime = 0;
    private void Update()
    {
        if (inited == false)
        {
            return;
        }

        //if ((Time.time - lastTime) > 0.25f)
        //{
        //    lastTime = Time.time;
        //    for (int i = 0; i < goals.Count; i++)
        //    {
        //        goals[i] += dirs[i] * 0.8f;
        //        Debug.DrawLine(goals[i].ToVec3XZ(),goals[i].ToVec3XZ()+Vector3.up*5f,Color.cyan,0.25f);
        //    }
        //}
        if (ReachedGoal() == false)
        {
            SetPreferredVelocities();
            Simulator.Instance.doStep();
        }
        for (int i = 0; i < agents.Count; i++)
        {
            vec2 = Simulator.Instance.getAgentPosition(i);
            realPos.x = vec2.x();
            realPos.z = vec2.y();
            vec2 = Simulator.Instance.getAgentVelocity(i);
            velocity.x = vec2.x_;
            velocity.z = vec2.y_;
            agentPos = agents[i].transform.position;
            Debug.DrawLine(agentPos + Vector3.up * 1f,
                agentPos + velocity.normalized * 2.5f + Vector3.up * 1f,
                Color.green, Time.deltaTime);
            Debug.DrawLine(realPos,realPos+Vector3.up*10,Color.red,Time.deltaTime);
            agents[i].transform.position = Vector3.Lerp(agentPos, realPos, Time.deltaTime *RVOMath.abs(Simulator.Instance.getAgentVelocity(i)));
        }
    }

    private Vector3 ToVec3(RVO.Vector2 vec)
    {
        return new Vector3(vec.x(),0,vec.y());
    }

    private Vector3[] ToVec3GizmoArray(List<Vector2> obstacle)
    {
        Vector3[] t = new Vector3[obstacle.Count+1];
        for (int i = 0; i < obstacle.Count; i++)
        {
            t[i] = ToVec3(obstacle[i]);
            if (i == obstacle.Count - 1)
            {
                t[i + 1] = ToVec3(obstacle[0]);
            }
        }
        return t;
    }
}
