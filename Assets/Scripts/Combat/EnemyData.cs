// #define DEBUG_COMPONENT_NOT_FOUND
// #define DEBUG_ENEMY_DATA
// #define DEBUG_STAT_CHANGE

/* 디버그 정의
 * DEBUG_COMPONENT_NOT_FOUND: 필수 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_ENEMY_DATA: 적 데이터 초기화 관련 디버그 정보를 출력
 * DEBUG_STAT_CHANGE: 스탯 변경 관련 디버그 정보를 출력
 */

using UnityEngine;

namespace Combat
{
    public class EnemyData : MonoBehaviour
    {
        [Header("기본 스탯")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float attackPower = 10f;
        [SerializeField] private float defense = 5f;

        private float currentHealth;
        private global::Enemy.Enemy enemyComponent;

        private void Start()
        {
            enemyComponent = GetComponent<global::Enemy.Enemy>();
            if (enemyComponent == null)
            {
                #if DEBUG_COMPONENT_NOT_FOUND
                Debug.LogError("Enemy 컴포넌트가 필요합니다!");
                #endif
                enabled = false;
                return;
            }

            InitializeStats();
        }

        private void InitializeStats()
        {
            currentHealth = maxHealth;
            #if DEBUG_ENEMY_DATA
            Debug.Log($"적 '{gameObject.name}'의 스탯 초기화");
            Debug.Log($"초기 체력: {maxHealth}, 공격력: {attackPower}, 방어력: {defense}");
            #endif
        }

        public void UpdateStats(float healthMod, float attackMod, float defenseMod)
        {
            maxHealth *= healthMod;
            attackPower *= attackMod;
            defense *= defenseMod;

            #if DEBUG_STAT_CHANGE
            Debug.Log($"적 '{gameObject.name}'의 스탯 업데이트");
            Debug.Log($"수정된 체력: {maxHealth}, 공격력: {attackPower}, 방어력: {defense}");
            #endif

            // 현재 체력도 최대 체력에 맞춰 조정
            currentHealth = maxHealth;
        }

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float AttackPower => attackPower;
        public float Defense => defense;

        public GameObject Enemy { get; private set; }
        public float Distance { get; private set; }
        public float Weight { get; set; }
        
        public EnemyData(GameObject enemy, float distance, float initialWeight = 1f)
        {
            Enemy = enemy;
            Distance = distance;
            Weight = initialWeight;
        }
        
        public void UpdateDistance(Vector3 fromPosition)
        {
            if (Enemy != null)
            {
                Distance = Vector3.Distance(fromPosition, Enemy.transform.position);
            }
        }
    }
} 