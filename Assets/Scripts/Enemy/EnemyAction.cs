using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Enemy Action", fileName = "NewEnemyAction")]
    public class EnemyAction : ScriptableObject
    {
        [Header("Identity")]
        public string actionName = "Attack";

        [Header("Intent Display")]
        [Tooltip("What the player sees telegraphed before this action fires.")]
        public EnemyIntent intentType = EnemyIntent.Attack;

        [Tooltip("If non-empty, shown as-is in the intent display instead of the auto-generated description. " +
                 "Useful when the action does multiple things (e.g. 'Ritual Ward  +12 Block and +2 Strength').")]
        public string customIntentDescription = "";

        [Header("Effects")]
        [Tooltip("Damage dealt to player. 0 = no damage.")]
        public int damage = 0;

        [Tooltip("Damage Type of attack")]
        public DamageType damageType;

        [Tooltip("Block the enemy gains before acting. Applied before dealing damage.")]
        public int selfBlock = 0;

        [Tooltip("Status effect applied to the player, if any.")]
        public StatusType applyStatus;
        public int  statusStacks    = 0;
        public bool appliesStatus   = false;

        [Tooltip("Status applied to self (buff), if any.")]
        public StatusType applySelfStatus;
        public int  selfStatusStacks   = 0;
        public bool appliesSelfStatus  = false;

        [Tooltip("How many times this action hits (e.g. 3 hits of 4 damage).")]
        [Range(1, 6)]
        public int hitCount = 1;

        [Header("Conditions")]
        [Tooltip("Minimum HP % required for this action to be valid. 0 = always valid.")]
        [Range(0f, 1f)]
        public float minHPPercent = 0f;

        [Tooltip("Maximum HP % for this action to be valid. 1 = always valid.")]
        [Range(0f, 1f)]
        public float maxHPPercent = 1f;

        public bool IsValidAt(float hpPercent)
        {
            return hpPercent >= minHPPercent && hpPercent <= maxHPPercent;
        }

        // Total damage output (for intent display)
        public int TotalDamage => damage * hitCount;
    }
}
