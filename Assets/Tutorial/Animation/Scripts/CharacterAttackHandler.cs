using UnityEngine;

public enum EComboType
{
    Sword_AAAAA,
    Sword_ABABA
}

public class ComboInput
{
    public EVirtualKeyType keyType;
    public int comboCount;
    public float expiredTime;
    public float timeStamp;
    public Vector2 allowedNormalizedRange;
}

public class CharacterAttackHandler : MonoBehaviour
{
    private Animator _animator;
    private int layer;
    private ComboInput previousInput;
    private ComboInput currentInput;
    private void Start()
    {
        _animator = GetComponent<Animator>();
        InputManager.Instance.Register(EInputType.Action, EVirtualKeyType.Attack_1, ListenAttackInput);
        InputManager.Instance.Register(EInputType.Action, EVirtualKeyType.Attack_2, ListenAttackInput);
        InputManager.Instance.Register(EInputType.Action, EVirtualKeyType.EquipWeapon, Equip);
        InputManager.Instance.Register(EInputType.Action, EVirtualKeyType.UnequipWeapon, Unequip);
        layer = _animator.GetLayerIndex("SwordAttack");
        previousInput = new ComboInput()
        {
            keyType = EVirtualKeyType.None,
            comboCount = -1
        };
    }

    private void ListenAttackInput(InputInfo inputKey)
    {
        currentInput = new ComboInput
        {
            keyType = inputKey.virtualKey,
            comboCount = previousInput.comboCount + 1,
            expiredTime =  2f,
            timeStamp = inputKey.timeStamp
        };
        UpdateCombo();
        previousInput = currentInput;
    }

    private void UpdateCombo()
    {
        if (currentInput.keyType == previousInput.keyType)
        {
            if (currentInput.timeStamp - previousInput.timeStamp < previousInput.expiredTime)
            {
                EnterComboStage(EComboType.Sword_AAAAA,currentInput.comboCount);
            }
            else
            {
                EnterComboStage(EComboType.Sword_AAAAA,0);
            }
        }
        else
        {
            EnterComboStage(EComboType.Sword_AAAAA,0);
        }
    }

    private void EnterComboStage(EComboType comboType, int comboCount)
    {
        if (comboCount == 0)
        {
            currentInput.comboCount = 0;
        }
        _animator.SetInteger("ComboCounter",comboCount);
        _animator.SetInteger("ComboType",(int)comboType);
    }

    private void Equip(InputInfo inputKey)
    {
        _animator.CrossFade("Withdrawing Sword", 0.5f);
    }
    private void Unequip(InputInfo inputKey)
    {
        _animator.CrossFade("Sheathing Sword", 0.5f);
    }

    private void Update()
    {
        
    }
}
