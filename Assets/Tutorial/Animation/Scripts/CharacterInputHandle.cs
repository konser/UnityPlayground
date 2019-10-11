using UnityEngine;
using System.Collections;

public class CharacterInputHandle
{
    private Character _character;
    public void RegisterInput(Character character)
    {
        _character = character;
        InputManager.Instance.Register(EInputType.State,EVirtualKeyType.MoveForward,OnMoveForward);
        InputManager.Instance.Register(EInputType.State, EVirtualKeyType.MoveBackward, OnMoveBackWard);
        InputManager.Instance.Register(EInputType.State, EVirtualKeyType.TurnLeft, OnTurnLeft);
        InputManager.Instance.Register(EInputType.State, EVirtualKeyType.TurnRight, OnTurnRight);
    }

    public void UnregisterInput()
    {
        InputManager.Instance.UnRegister(EInputType.State, EVirtualKeyType.MoveForward, OnMoveForward);
        InputManager.Instance.UnRegister(EInputType.State, EVirtualKeyType.MoveBackward, OnMoveBackWard);
        InputManager.Instance.UnRegister(EInputType.State, EVirtualKeyType.TurnLeft, OnTurnLeft);
        InputManager.Instance.UnRegister(EInputType.State, EVirtualKeyType.TurnRight, OnTurnRight);
    }

    private void OnMoveForward(InputData data)
    {
        if (data.isReleased)
        {
            _character.SetForwardVelocity(0f);
        }
        else
        {
            _character.SetForwardVelocity(Input.GetAxis("Vertical"));
        }
    }

    private void OnMoveBackWard(InputData data)
    {
        if (data.isReleased)
        {
            _character.SetForwardVelocity(0f);
        }
        else
        {
            _character.SetForwardVelocity(Input.GetAxis("Vertical"));
        }
    }

    private void OnTurnLeft(InputData data)
    {
        if (data.isReleased)
        {
            _character.SetTurnAround(0f);
        }
        else
        {
            _character.SetTurnAround(Input.GetAxis("Horizontal"));
        }
    }

    private void OnTurnRight(InputData data)
    {
        if (data.isReleased)
        {
            _character.SetTurnAround(0f);
        }
        else
        {
            _character.SetTurnAround(Input.GetAxis("Horizontal"));
        }
    }

}
