using System;
using UnityEngine;

[ExecuteInEditMode]
public class InfoToReflMaterial : MonoBehaviour
{
    // The proxy volume used for local reflection calculations.
    public BoxCollider collider;
    private Renderer r;
    void Start()
    {
        // Min and max BBox points in world coordinates.
        // Pass the values to the material.
        Renderer r = gameObject.GetComponent<Renderer>();

    }

    private void Update()
    {
        if (r == null)
        {
            r = gameObject.GetComponent<Renderer>();
        }
        Vector3 BMin = collider.bounds.min;
        Vector3 BMax = collider.bounds.max;
        r.sharedMaterial.SetVector("_BBoxMin", BMin);
        r.sharedMaterial.SetVector("_BBoxMax", BMax);
        r.sharedMaterial.SetVector("_EnviCubeMapPos", collider.bounds.center);
    }
}