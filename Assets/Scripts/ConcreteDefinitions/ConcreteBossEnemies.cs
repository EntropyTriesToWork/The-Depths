using System.Collections.Generic;

namespace CardGame
{
    public class TheGuardianBoss : EnemyDefinition
    {
        public TheGuardianBoss()
        {
            EnemyID   = "the_guardian";
            EnemyName = "The Guardian";
            BaseHP    = 100;
            IsBoss    = true;
            Actions   = new List<EnemyActionDefinition>
            {
                // Turn 1: raise defenses
                new EnemyActionDefinition
                {
                    ActionName             = "Defensive Mode",
                    IntentType             = EnemyIntent.Defend,
                    SelfBlock              = 20,
                    CustomIntentDescription = "Defensive Mode  +20 Block"
                },
                // Turn 2: punishing attack
                new EnemyActionDefinition
                {
                    ActionName = "Shield Bash",
                    IntentType = EnemyIntent.Attack,
                    Damage     = 10,
                    HitCount   = 1
                },
                // Turn 3: big hit
                new EnemyActionDefinition
                {
                    ActionName = "Crush",
                    IntentType = EnemyIntent.Attack,
                    Damage     = 18,
                    HitCount   = 1
                },
                // Turn 4: weaken
                new EnemyActionDefinition
                {
                    ActionName             = "Iron Wall",
                    IntentType             = EnemyIntent.Debuff,
                    ApplyStatus            = StatusType.Weak,
                    StatusStacks           = 3,
                    CustomIntentDescription = "Iron Wall  Apply 3 Weak"
                },
                // Turn 5: back to heavy attack, cycle repeats
                new EnemyActionDefinition
                {
                    ActionName = "Crush",
                    IntentType = EnemyIntent.Attack,
                    Damage     = 18,
                    HitCount   = 1
                }
            };
        }
    }

    public class CorruptedSeerBoss : EnemyDefinition
    {
        public CorruptedSeerBoss()
        {
            EnemyID   = "corrupted_seer";
            EnemyName = "Corrupted Seer";
            BaseHP    = 85;
            IsBoss    = true;
            Actions   = new List<EnemyActionDefinition>
            {
                new EnemyActionDefinition
                {
                    ActionName             = "Dark Ritual",
                    IntentType             = EnemyIntent.Buff,
                    ApplyStatus            = StatusType.Strength,
                    StatusStacks           = 2,
                    CustomIntentDescription = "Dark Ritual  +2 Strength"
                },
                new EnemyActionDefinition
                {
                    ActionName             = "Hex Bolt",
                    IntentType             = EnemyIntent.Attack,
                    Damage                 = 8,
                    HitCount               = 2,
                },
                new EnemyActionDefinition
                {
                    ActionName             = "Wither",
                    IntentType             = EnemyIntent.Debuff,
                    ApplyStatus            = StatusType.Exposed,
                    StatusStacks           = 3,
                    CustomIntentDescription = "Wither  Apply 3 Exposed"
                },
                new EnemyActionDefinition
                {
                    ActionName = "Soul Drain",
                    IntentType = EnemyIntent.Attack,
                    Damage     = 22,
                    HitCount   = 1
                }
            };
        }
    }

    public static class ConcreteBossEnemies
    {
        public static List<EnemyDefinition> All() => new List<EnemyDefinition>
        {
            new TheGuardianBoss(),
            new CorruptedSeerBoss()
        };
    }
}
