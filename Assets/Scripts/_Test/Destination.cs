using UnityEngine;
using System.Collections;

public class Destination : MonoBehaviour
{
    public static Destination instance;
    // Use this for initialization
    void Start()
    {
        instance = this;
    }

    public Vector3 GetPosition()
    {
        return this.transform.position;
    }
    // Update is called once per frame
    void Update()
    {

    }
}
