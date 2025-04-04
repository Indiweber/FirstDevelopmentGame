using UnityEngine;

namespace Enemy
{
    public class Enemy : MonoBehaviour
    {
        private void OnEnable()
        {
            AutoCombat.RegisterEnemy(gameObject);
        }

        private void OnDisable()
        {
            AutoCombat.UnregisterEnemy(gameObject);
        }

        private void OnDestroy()
        {
            AutoCombat.UnregisterEnemy(gameObject);
        }
    }
} 