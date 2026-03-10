using System.Collections.Generic;

namespace CardGame
{
    public class SlimeKingEnemy : EnemyDefinition
    {
        public SlimeKingEnemy()
        {
            EnemyID   = "slime_king";
            EnemyName = "Slime King";
            BaseHP    = 50;
            IsElite   = true;
            Actions   = new List<EnemyActionDefinition>
            {
                new EnemyActionDefinition
                {
                    ActionName = "Engulf",
                    IntentType = EnemyIntent.Attack,
                    Damage     = 12,
                    HitCount   = 1
                },
                new EnemyActionDefinition
                {
                    ActionName             = "Split",
                    IntentType             = EnemyIntent.Buff,
                    ApplyStatus            = StatusType.Strength,
                    StatusStacks           = 2,
                    CustomIntentDescription = "Split  +2 Strength"
                },
                new EnemyActionDefinition
                {
                    ActionName = "Slam",
                    IntentType = EnemyIntent.Attack,
                    Damage     = 8,
                    HitCount   = 2
                }
            };
        }
    }

    public class DarkPriestEnemy : EnemyDefinition
    {
        public DarkPriestEnemy()
        {
            EnemyID   = "dark_priest";
            EnemyName = "Dark Priest";
            BaseHP    = 45;
            IsElite   = true;
            Actions   = new List<EnemyActionDefinition>
            {
                new EnemyActionDefinition
                {
                    ActionName             = "Curse",
                    IntentType             = EnemyIntent.Debuff,
                    ApplyStatus            = StatusType.Exposed,
                    StatusStacks           = 2,
                    CustomIntentDescription = "Curse  Apply 2 Exposed"
                },
                new EnemyActionDefinition
                {
                    ActionName = "Dark Beam",
                    IntentType = EnemyIntent.Attack,
                    Damage     = 10,
                    HitCount   = 1
                },
                new EnemyActionDefinition
                {
                    ActionName             = "Ritual Ward",
                    IntentType             = EnemyIntent.Defend,
                    SelfBlock              = 12,
                    CustomIntentDescription = "Ritual Ward  +12 Block"
                }
            };
        }
    }

    public class IronGolemElite : EnemyDefinition
    {
        public IronGolemElite()
        {
            EnemyID   = "iron_golem";
            EnemyName = "Iron Golem";
            BaseHP    = 60;
            IsElite   = true;
            Actions   = new List<EnemyActionDefinition>
            {
                new EnemyActionDefinition
                {
                    ActionName             = "Iron Skin",
                    IntentType             = EnemyIntent.Defend,
                    SelfBlock              = 10,
                    CustomIntentDescription = "Iron Skin  +10 Block"
                },
                new EnemyActionDefinition
                {
                    ActionName = "Crush",
                    IntentType = EnemyIntent.Attack,
                    Damage     = 14,
                    HitCount   = 1
                },
                new EnemyActionDefinition
                {
                    ActionName = "Ground Slam",
                    IntentType = EnemyIntent.Attack,
                    Damage     = 6,
                    HitCount   = 3
                }
            };
        }
    }

    public static class ConcreteEliteEnemies
    {
        public static List<EnemyDefinition> All() => new List<EnemyDefinition>
        {
            new SlimeKingEnemy(),
            new DarkPriestEnemy(),
            new IronGolemElite()
        };
    }
}
