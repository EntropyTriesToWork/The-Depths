using System;

[Serializable]
public struct EffectParameters
{
    // Common numeric values
    public int intValue;
    public float floatValue;
    public string stringValue;

    public int manaGain;
    public int energyGain;

    public TargetType targetType;
    public StatusType statusType;

    public bool isExhaust;

    public EffectParameters(
        int intVal = 0,
        float floatVal = 0f,
        string stringVal = "",
        int manaGain = 0,
        int energyGain = 0,
        TargetType target = TargetType.SingleEnemy,
        StatusType status = StatusType.None,
        bool exhaust = false)
    {
        intValue = intVal;
        floatValue = floatVal;
        stringValue = stringVal;
        this.manaGain = manaGain;
        this.energyGain = energyGain;
        targetType = target;
        statusType = status;
        isExhaust = exhaust;
    }
}

public enum TargetType { Self, SingleEnemy, AllEnemies, RandomEnemy, AllCharacters }
public enum StatusType { None, Poison, Weak, Vulnerable, Strength, Dexterity }