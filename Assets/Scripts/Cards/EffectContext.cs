using System.Collections.Generic;

public class EffectContext
{
    public Entity source;                
    public List<Entity> targets;         
    public object customData;            

    public EffectContext(Entity source, List<Entity> targets)
    {
        this.source = source;
        this.targets = targets;
    }
}