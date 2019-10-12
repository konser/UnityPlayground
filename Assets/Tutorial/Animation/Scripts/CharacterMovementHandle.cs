using UnityEngine;

public class CharacterMovementHandle : MonoBehaviour
{
    public Transform groundChecker;
    public float groundCheckDistance;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    public float runSpeed = 3f;
    private Animator _animator;
    private CharacterController _controller;
    private CharacterMovementHandle _movementHandle;

    [Header("For Debugging")]
    [SerializeField]
    private bool _isGrounded;
    public float _forwardVal;
    public float _leftRightVal;
    public bool _isShiftHolded;
    public bool _mouseRightHolded;
    public Vector3 velocity;
    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _movementHandle = GetComponent<CharacterMovementHandle>();

        InputManager.Instance.Register(EInputType.State,EVirtualKeyType.MoveForward,ListenInput);
        InputManager.Instance.Register(EInputType.State, EVirtualKeyType.MoveBackward, ListenInput);
        InputManager.Instance.Register(EInputType.State, EVirtualKeyType.MoveLeft, ListenInput);
        InputManager.Instance.Register(EInputType.State, EVirtualKeyType.MoveRight, ListenInput);
        InputManager.Instance.Register(EInputType.State, EVirtualKeyType.LShift, ListenInput);
        InputManager.Instance.Register(EInputType.State,EVirtualKeyType.RightMouse,ListenInput);
        InputManager.Instance.Register(EInputType.Action, EVirtualKeyType.Jump, ListenInput);

    }

    private void ListenInput(InputData inputKey)
    {
        if (inputKey.inputType == EInputType.State)
        {
            switch (inputKey.virtualKey)
            {
                case EVirtualKeyType.LShift:
                    _isShiftHolded = !inputKey.isReleased;
                    break;
                case EVirtualKeyType.MoveForward:
                    _forwardVal = inputKey.isReleased? 0f : _isShiftHolded ? 1f / runSpeed : 1f;
                    break;
                case EVirtualKeyType.MoveBackward:
                    _forwardVal = -(inputKey.isReleased ? 0f : _isShiftHolded ? 1f / runSpeed : 1f);
                    break;
                case EVirtualKeyType.MoveLeft:
                    _leftRightVal = -(inputKey.isReleased ? 0f : _isShiftHolded ? 1f / runSpeed : 1f);
                    break;
                case EVirtualKeyType.MoveRight:
                    _leftRightVal = inputKey.isReleased ? 0f : _isShiftHolded ? 1f / runSpeed : 1f;
                    break;
                case EVirtualKeyType.RightMouse:
                    _mouseRightHolded = !inputKey.isReleased;
                    break;
            }
        }
        else
        {

        }
    }



    private void Update()
    {
        // orientation
        RotateCharacter();

        // velocity
        Vector3 dir = GetMoveDirection();
        velocity.x = dir.x * Mathf.Abs(_leftRightVal) * runSpeed;
        velocity.z = dir.z * Mathf.Abs(_forwardVal) * runSpeed;
        velocity.y += gravity*Time.deltaTime;
        velocity = transform.TransformDirection(velocity);
        _isGrounded = Physics.CheckSphere(groundChecker.position, groundCheckDistance, 1 << LayerMask.NameToLayer("Obstacle"), QueryTriggerInteraction.Ignore);
        if (_isGrounded && velocity.y < 0)
        {
            velocity.y = 0;
        }
        _controller.Move(velocity * Time.deltaTime);

        // animation
        _animator.SetFloat("XSpeed", _leftRightVal);
        _animator.SetFloat("ZSpeed", _forwardVal);
    }

    private Vector3 GetMoveDirection()
    {
        return new Vector3(Input.GetAxis("Horizontal"),0f,Input.GetAxis("Vertical"));
    }

    private void RotateCharacter()
    {
        if (_mouseRightHolded)
        {
            transform.forward = Camera.main.transform.forward.XZ();
        }
    }
}
