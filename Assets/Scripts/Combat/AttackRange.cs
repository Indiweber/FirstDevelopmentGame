// #define DEBUG_COMPONENT_NOT_FOUND
// #define DEBUG_ATTACK_RANGE
// #define DEBUG_ENEMY_MANAGEMENT

/* 디버그 정의
 * DEBUG_COMPONENT_NOT_FOUND: 필수 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_ATTACK_RANGE: 공격 범위 관련 디버그 정보를 출력
 * DEBUG_ENEMY_MANAGEMENT: 적 등록/해제 관련 디버그 정보를 출력
 */

using UnityEngine;
using System.Collections.Generic;

public class AttackRange : MonoBehaviour
{
    private HashSet<GameObject> enemiesInRange = new HashSet<GameObject>();
    private AutoCombat autoCombat;
    
    [SerializeField] private float attackRadius = 2f;
    [SerializeField] private LayerMask enemyLayer;
    
    private void Start()
    {
        autoCombat = GetComponentInParent<AutoCombat>();
        if (autoCombat == null)
        {
            #if DEBUG_COMPONENT_NOT_FOUND
            Debug.LogError("AutoCombat 컴포넌트를 찾을 수 없습니다!");
            #endif
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesInRange.Add(other.gameObject);
            #if DEBUG_ATTACK_RANGE
            Debug.Log($"적이 공격 범위에 진입: {other.gameObject.name}");
            #endif
            autoCombat?.OnEnemyEnteredAttackRange(other.gameObject);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesInRange.Remove(other.gameObject);
            #if DEBUG_ATTACK_RANGE
            Debug.Log($"적이 공격 범위를 벗어남: {other.gameObject.name}");
            #endif
            autoCombat?.OnEnemyExitedAttackRange(other.gameObject);
        }
    }
    
    public void ValidateEnemiesInRange()
    {
        var invalidEnemies = new List<GameObject>();
        
        foreach (var enemy in enemiesInRange)
        {
            if (enemy == null || !enemy.activeInHierarchy)
            {
                invalidEnemies.Add(enemy);
                #if DEBUG_ENEMY_MANAGEMENT
                Debug.Log($"유효하지 않은 적 감지됨: {enemy?.name ?? "null"}");
                #endif
            }
        }
        
        foreach (var enemy in invalidEnemies)
        {
            enemiesInRange.Remove(enemy);
            #if DEBUG_ENEMY_MANAGEMENT
            Debug.Log($"유효하지 않은 적 제거됨: {enemy?.name ?? "null"}");
            #endif
            autoCombat?.OnEnemyExitedAttackRange(enemy);
        }
    }
    
    public bool HasEnemiesInRange()
    {
        return enemiesInRange.Count > 0;
    }
    
    public HashSet<GameObject> GetEnemiesInRange()
    {
        return new HashSet<GameObject>(enemiesInRange);
    }
} 