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
        if (movementReq.velocity != Vector3.zero)
        {
            transform.forward = Vector3.Lerp(transform.forward, movementReq.velocity.normalized, Time.deltaTime);
        }
    }
    public void InitAgent(Vector3 pos)
    {
        this.transform.position = pos;
    }

    public void Test()
    {
       NavManager.instance.RequestNavigation(individual,Destination.instance.GetPosition());
    }

}
