using UnityEngine;
using System.Collections;
using RVO;

public class GroupAgent : MonoBehaviour,INavAgent
{
    public int totalAgent=16;
    public int rowCount=4;
    public float radius=2;

    private NavGroup group
    {
        get { return (NavGroup)entity; }
    }

    public float collisionRadius
    {
        get { return radius - 1f; }
    }
    private NavEntity entity;

    // Use this for initialization
    void Start()
    {
        entity = new NavGroup(this);
        CreateFormation();
    }

    // Update is called once per frame
    void Update()
    {

    }
    #region NavAgent Interface

    private void CreateFormation()
    {
        for (int i = 0; i < totalAgent; i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            NavIndividual individual = go.AddComponent<IndividualAgent>().individual;
            group.individualList.Add(individual);
        }
        SquareFormation f = new SquareFormation();
        f.InitFormation(this.transform.position,this.transform.forward,totalAgent,rowCount,radius);
        group.AssignFormation(f);
    }

    public Vector3 GetCurrentPosition()
    {
        return transform.position;
    }
    public Vector3 GetForward()
    {
        return this.transform.forward;
    }

    public void AgentMove(MovementRequest movementReq)
    {
        //Debug.Log(movementReq.velocity);
        transform.position += movementReq.velocity * Time.deltaTime;
        transform.forward = movementReq.velocity.normalized;
        group.DebugDraw();
    }
    public void InitAgent(Vector3 pos)
    {
        this.transform.position = pos;
    }

    [Sirenix.OdinInspector.Button]
    public void Test()
    {
        NavManager.instance.RequestNavigation(group, Destination.instance.GetPosition());
    }
    #endregion
}
