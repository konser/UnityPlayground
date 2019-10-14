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

    private void Attack1(InputData inputKey)
    {
        if(_animator.GetCurrentAnimatorStateInfo(_animator.GetLayerIndex("SwordAttack")).normalizedTime >= 1.0f)
         _animator.Play("Stable Sword Inward Slash",-1,0f);
    }


    private void Attack2(InputData inputKey)
    {
        _animator.CrossFade("Stable Sword Outward Slash",0f);
    }
    private void Equip(InputData inputKey)
    {
        _animator.CrossFade("Withdrawing Sword", 0.5f);
    }
    private void Unequip(InputData inputKey)
    {
        _animator.CrossFade("Sheathing Sword", 0.5f);
    }

    private void Update()
    {
        
    }
}
