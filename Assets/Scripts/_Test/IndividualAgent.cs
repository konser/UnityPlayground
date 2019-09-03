using System;
using UnityEngine;
using System.Collections;

public class IndividualAgent : MonoBehaviour,INavAgent
{
    public NavIndividual individual;
    private void Awake()
    {
        individual = new NavIndividual(this);
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
        transform.position += movementReq.velocity * Time.deltaTime;
    }
    public void InitAgent(Vector3 pos)
    {
        this.transform.position = pos;
    }

    [Sirenix.OdinInspector.Button]
    public void Test()
    {
       NavManager.instance.RequestNavigation(individual,Destination.instance.GetPosition());
    }

}
