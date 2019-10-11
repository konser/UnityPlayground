using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public Transform groundChecker;
    public float groundCheckDistance;
    public float gravity = -9.81f;
    private Animator _animator;
    private CharacterController _controller;
    private CharacterInputHandle _inputHandle;
    [SerializeField]
    private bool _isGrounded;
    [SerializeField]
    private Vector3 _velocity;

    private void Start()
    {
        _controller = this.GetComponent<CharacterController>();
        _animator = this.GetComponent<Animator>();
        _inputHandle = new CharacterInputHandle();
        _inputHandle.RegisterInput(this);
    }

    public void SetVelocity(Vector3 velocity)
    {
        _velocity = velocity;
    }

    public void SetForwardVelocity(float value)
    {
        _animator.SetFloat("ZSpeed",value);
    }

    public void SetTurnAround(float value)
    {
        _animator.SetFloat("XSpeed", value);
    }

    private void Update()
    {
        _velocity.y += gravity * Time.deltaTime;
        // 在地面上时要重置Y轴的速度
        _isGrounded = Physics.CheckSphere(groundChecker.position, groundCheckDistance, 1 << LayerMask.NameToLayer("Ground"),QueryTriggerInteraction.Ignore);
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = 0;
        }
        _controller.Move(_velocity*Time.deltaTime);
    }
}
