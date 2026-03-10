using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Enemy Data", fileName = "NewEnemy")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string enemyName  = "Unknown Enemy";
        public string enemyID    = "";
        public bool   isElite    = false;
        public bool   isBoss     = false;

        [Header("Stats")]
        public int baseHP        = 20;
        public int baseBlock     = 0;        // Some enemies start with block

        [Header("Intent Pattern")]
        [Tooltip("The ordered list of actions this enemy cycles through.")]
        public EnemyAction[] actionPattern  = new EnemyAction[0];

        [Tooltip("If true, the enemy picks a random action each turn instead of cycling.")]
        public bool randomPattern           = false;

        [Header("Visuals")]
        public Sprite idleSprite;
        public Sprite attackSprite;
        public Sprite hurtSprite;

        [Header("Unlock")]
        public string unlockID = "";
    }

    // Intent icons shown to the player
    public enum EnemyIntent
    {
        Attack,           // Sword icon
        AttackAndDebuff,  // Sword + skull
        AttackAndBuff,    // Sword + arrow up
        Defend,           // Shield icon
        Buff,             // Arrow up icon
        Debuff,           // Skull icon
        Summon,           // Minions icon
        Unknown           // Question mark (used for boss telegraphs)
    }
}
