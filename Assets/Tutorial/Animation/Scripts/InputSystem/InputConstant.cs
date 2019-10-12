using UnityEngine;
using System.Collections;

public enum EInputType
{
    Action,
    State,
    Range
}

public enum EVirtualKeyType
{
    MoveLeft=100,
    MoveRight,
    MoveForward,
    MoveBackward,
    TurnLeft,
    TurnRight,
    Jump=200,
    Crouch,

    Attack_1 =300,
    Attack_2,
    Attack_3,
    Attack_4,
    Attack_5,
    Attack_6,
    Attack_7,
    Attack_8,
    EquipWeapon,
    UnequipWeapon,

    Skill_1=400,
    Skill_2,
    Skill_3,
    Skill_4,
    Skill_5,
    Skill_6,
    Skill_7,
    Skill_8,
    // 辅助
    LShift = 900,
    RightMouse 
}

public enum EInputContextType
{
    Prioirty,
    NormalHumanPlayer
}
public struct InputData
{
    public EInputType inputType;
    public EVirtualKeyType virtualKey;
    public float holdTime;
    public float normalizeValue;
    public bool isReleased;

    public override string ToString()
    {
        string msg = "";
        if (inputType == EInputType.State)
        {
            msg += isReleased ? "释放" : "按下";
        }
        return $"指令 {virtualKey} 输入类型 {inputType} {msg}";
    }
}

public static class InputConstant
{
    /// <summary>
    /// 判定为持续按键的最低时间（60fps下约为5帧）
    /// </summary>
    public const float MIN_HOLD_TIME = 0.08f;
}
