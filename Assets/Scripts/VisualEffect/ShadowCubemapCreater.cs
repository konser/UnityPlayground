using System;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
[ExecuteInEditMode]
public class ShadowCubemapCreater : MonoBehaviour
{
    public BoxCollider boxCollider;
    public Vector3 boundMin;
    public Vector3 boundMax;
    public Vector3 center;
    private void Awake()
    {
        boxCollider = this.GetComponent<BoxCollider>();

    }

    private void Update()
    {
        SetBoundValue();
    }

    private void SetBoundValue()
    {
        boundMin = boxCollider.bounds.min;
        boundMax = boxCollider.bounds.max;
        center = boxCollider.bounds.center;
    }
}
