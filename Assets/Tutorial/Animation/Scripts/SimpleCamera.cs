using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCamera : MonoBehaviour
{
    public float backOffset;
    public float heightOffset;
    public float speed;

    public Transform followedTarget;
    private Transform _cachedCamera;
    private Vector3 _targetCamPos;

    private void Start()
    {
        _cachedCamera = Camera.main.transform;
        followedTarget = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void LateUpdate()
    {
        _targetCamPos = followedTarget.position + Vector3.up * heightOffset + (-followedTarget.forward) * backOffset;
        _cachedCamera.transform.position = Vector3.Lerp(_cachedCamera.transform.position, _targetCamPos, Time.deltaTime * speed);
        _cachedCamera.transform.LookAt(followedTarget,Vector3.up);
    }
}