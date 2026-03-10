using System.Collections.Generic;

namespace CardGame
{
    public class SlimeEnemy : EnemyDefinition
    {
        public SlimeEnemy()
        {
            EnemyID   = "slime";
            EnemyName = "Slime";
            BaseHP    = 24;
            Actions   = new List<EnemyActionDefinition>
            {
                new EnemyActionDefinition
                {
                    ActionName = "Chomp",
                    IntentType = EnemyIntent.Attack,
                    Damage     = 6,
                    HitCount   = 1
                },
                new EnemyActionDefinition
                {
                    ActionName             = "Ooze",
                    IntentType             = EnemyIntent.Defend,
                    SelfBlock              = 5,
                    CustomIntentDescription = "Ooze  +5 Block"
                }
            };
        }
    }

    public class CultistEnemy : EnemyDefinition
    {
        public CultistEnemy()
        {
            EnemyID   = "cultist";
            EnemyName = "Cultist";
            BaseHP    = 18;
            Actions   = new List<EnemyActionDefinition>
            {
                new EnemyActionDefinition
                {
                    ActionName             = "Ritual",
                    IntentType             = EnemyIntent.Buff,
                    ApplyStatus            = StatusType.Strength,
                    StatusStacks           = 3,
                    CustomIntentDescription = "Ritual  +3 Strength"
                },
                new EnemyActionDefinition
                {
                    ActionName = "Dark Strike",
                    IntentType = EnemyIntent.Attack,
                    Damage     = 5,
                    HitCount   = 2
                }
            };
        }
    }

    public class RatEnemy : EnemyDefinition
    {
        public RatEnemy()
        {
            EnemyID   = "rat";
            EnemyName = "Rat";
            BaseHP    = 14;
            Actions   = new List<EnemyActionDefinition>
            {
                new EnemyActionDefinition
                {
                    ActionName = "Bite",
                    IntentType = EnemyIntent.Attack,
                    Damage     = 4,
                    HitCount   = 1
                },
                new EnemyActionDefinition
                {
                    ActionName = "Scratch",
                    IntentType = EnemyIntent.Attack,
                    Damage     = 3,
                    HitCount   = 2
                }
            };
        }
    }

    public class GoblinEnemy : EnemyDefinition
    {
        public GoblinEnemy()
        {
            EnemyID   = "goblin";
            EnemyName = "Goblin";
            BaseHP    = 20;
            Actions   = new List<EnemyActionDefinition>
            {
                new EnemyActionDefinition
                {
                    ActionName             = "Sneak",
                    IntentType             = EnemyIntent.Debuff,
                    ApplyStatus            = StatusType.Weak,
                    StatusStacks           = 2,
                    CustomIntentDescription = "Sneak  Apply 2 Weak"
                },
                new EnemyActionDefinition
                {
                    ActionName = "Backstab",
                    IntentType = EnemyIntent.Attack,
                    Damage     = 8,
                    HitCount   = 1
                }
            };
        }
    }

    // ----------------------------------------------------------
    //  Registry
    // ----------------------------------------------------------

    public static class ConcreteEnemies
    {
        public static List<EnemyDefinition> All() => new List<EnemyDefinition>
        {
            new SlimeEnemy(),
            new CultistEnemy(),
            new RatEnemy(),
            new GoblinEnemy()
        };
    }
}
