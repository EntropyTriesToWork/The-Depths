using UnityEngine;

public abstract class Effect : ScriptableObject
{
    [TextArea(3, 5)]
    public string descriptionFormat = "Does something.";

    public abstract void Apply(EffectContext context, EffectParameters parameters);
}