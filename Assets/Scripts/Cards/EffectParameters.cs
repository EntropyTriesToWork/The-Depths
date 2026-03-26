using System;

[Serializable]
public struct EffectParameters
{
    // Common numeric values
    public int intValue;
    public float floatValue;
    public string stringValue;

    public int manaGain;

    // Targeting and status info
    public TargetType targetType;
    public StatusType statusType;

    // Flags for special behaviour
    public bool isExhaust;
    public bool upgradeAffectsValue;

    // Convenience constructor
    public EffectParameters(
        int intVal = 0,
        float floatVal = 0f,
        string stringVal = "",
        int manaGain = 0,
        TargetType target = TargetType.SingleEnemy,
        StatusType status = StatusType.None,
        bool exhaust = false,
        bool upgrade = false)
    {
        intValue = intVal;
        floatValue = floatVal;
        stringValue = stringVal;
        this.manaGain = manaGain;
        targetType = target;
        statusType = status;
        isExhaust = exhaust;
        upgradeAffectsValue = upgrade;
    }
}

public enum TargetType { Self, SingleEnemy, AllEnemies, RandomEnemy, AllCharacters }
public enum StatusType { None, Poison, Weak, Vulnerable, Strength, Dexterity }