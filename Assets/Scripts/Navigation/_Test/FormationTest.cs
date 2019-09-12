using System;
using UnityEngine;
using System.Collections;

public class FormationTest : MonoBehaviour
{
    private SquareFormation f;
    public int totalAgent;
    public int rowCount;
    public float radius;
    public void Test()
    {
        f = new SquareFormation();
        f.InitFormation(this.transform.position, this.transform.forward, totalAgent, rowCount, radius);
    }

    private void Update()
    {
        if (f == null)
        {
            return;
        }
        f.SetCenter(this.transform.position);
        f.SetForward(this.transform.forward);
        f.CaculateSlotPosition();
        f.DebugDrawer();
    }
}
