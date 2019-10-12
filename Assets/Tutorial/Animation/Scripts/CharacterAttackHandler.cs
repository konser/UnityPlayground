using UnityEngine;
using System.Collections;

public class CharacterAttackHandler : MonoBehaviour
{
    private Animator _animator;
    private int layer;
    private void Start()
    {
        _animator = GetComponent<Animator>();
        InputManager.Instance.Register(EInputType.Action, EVirtualKeyType.Attack_1, Attack1);
        InputManager.Instance.Register(EInputType.Action, EVirtualKeyType.Attack_2, Attack2);
        InputManager.Instance.Register(EInputType.Action, EVirtualKeyType.EquipWeapon, Equip);
        InputManager.Instance.Register(EInputType.Action, EVirtualKeyType.UnequipWeapon, Unequip);
        layer = _animator.GetLayerIndex("SwordAttack");
    }

    private void ListenInput(InputData inputKey)
    {
        if (inputKey.inputType == EInputType.Action)
        {
            switch (inputKey.virtualKey)
            {
            }
        }
    }

    private void Attack1(InputData inputKey)
    {
 
    }


    private void Attack2(InputData inputKey)
    {

    }
    private void Equip(InputData inputKey)
    {
        _animator.CrossFade("Withdrawing Sword", 0f);
    }
    private void Unequip(InputData inputKey)
    {
        _animator.CrossFade("Sheathing Sword", 0f);
    }

    private void Update()
    {
        
    }



}
