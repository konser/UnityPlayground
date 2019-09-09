using UnityEngine;
using System.Collections;

public class NavIndividual : NavEntity
{

    public override ENavEntityType navEntityType
    {
        get { return ENavEntityType.Individual; }
    }

    public NavIndividual(INavAgent controlledTarget) : base(controlledTarget)
    {
    }
}
