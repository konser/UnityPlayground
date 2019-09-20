using System;
using UnityEngine;
using System.Collections;

public class LocalCubeMap : MonoBehaviour
{
    public Transform light;
    public Material mat;
    public Vector3 min;
    public Vector3 max;
    public Vector3 originPos;
    private BoxCollider collider;
    private void Start()
    {
    }

    private void Update()
    {
        if (collider == null)
        {
            collider = gameObject.GetComponent<BoxCollider>();
        }
        originPos = collider.bounds.center;
        min = collider.bounds.min;
        max = collider.bounds.max;
        // lightPos = light.transform.position;
        mat.SetVector("_BoundMin", min);
        mat.SetVector("_BoundMax", max);
        mat.SetVector("_CubePos", originPos);
        //lightPos = light.transform.position;
        mat.SetVector("_LightPos",light.transform.position);
    }
}
