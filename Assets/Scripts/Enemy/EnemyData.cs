using UnityEngine;

namespace CardGame
{
    public class EnemyData : ScriptableObject
    {
        public string        enemyID;
        public string        enemyName;
        public int           baseHP;
        public bool          isElite;
        public bool          isBoss;
        public EnemyAction[] actionPattern;
    }
}
