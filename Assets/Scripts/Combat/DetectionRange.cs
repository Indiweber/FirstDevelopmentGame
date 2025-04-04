// #define DEBUG_COMPONENT_NOT_FOUND
// #define DEBUG_DETECTION
// #define DEBUG_TARGET_MANAGEMENT

/* 디버그 정의
 * DEBUG_COMPONENT_NOT_FOUND: 필수 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_DETECTION: 감지 범위 관련 디버그 정보를 출력
 * DEBUG_TARGET_MANAGEMENT: 대상 관리 관련 디버그 정보를 출력
 */

using UnityEngine;
using System.Collections.Generic;

public class DetectionRange : MonoBehaviour
{
    private HashSet<GameObject> detectedEnemies = new HashSet<GameObject>();
    private AutoCombat autoCombat;
    
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private LayerMask enemyLayer;
    
    private void Awake()
    {
        autoCombat = GetComponentInParent<AutoCombat>();
        if (autoCombat == null)
        {
            #if DEBUG_COMPONENT_NOT_FOUND
            Debug.LogError("DetectionRange: AutoCombat 컴포넌트를 찾을 수 없습니다!");
            #endif
        }
    }
    
    private void Start()
    {
        if (GetComponent<SphereCollider>() == null)
        {
            #if DEBUG_COMPONENT_NOT_FOUND
            Debug.LogError("SphereCollider 컴포넌트가 필요합니다!");
            #endif
            enabled = false;
        }

        if (autoCombat == null)
        {
            #if DEBUG_COMPONENT_NOT_FOUND
            Debug.LogError("CombatManager가 설정되지 않았습니다!");
            #endif
            enabled = false;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            detectedEnemies.Add(other.gameObject);
            #if DEBUG_DETECTION
            Debug.Log($"적 감지됨: {other.gameObject.name}");
            #endif
            autoCombat.OnEnemyDetected(other.gameObject);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            detectedEnemies.Remove(other.gameObject);
            #if DEBUG_DETECTION
            Debug.Log($"적 감지 해제됨: {other.gameObject.name}");
            #endif
            autoCombat.OnEnemyLost(other.gameObject);
        }
    }
    
    public void ValidateDetectedEnemies()
    {
        var invalidEnemies = new List<GameObject>();
        
        foreach (var enemy in detectedEnemies)
        {
            if (enemy == null || !enemy.activeInHierarchy)
            {
                invalidEnemies.Add(enemy);
                #if DEBUG_TARGET_MANAGEMENT
                Debug.Log($"유효하지 않은 적 감지됨: {enemy?.name ?? "null"}");
                #endif
            }
        }
        
        foreach (var enemy in invalidEnemies)
        {
            detectedEnemies.Remove(enemy);
            #if DEBUG_TARGET_MANAGEMENT
            Debug.Log($"유효하지 않은 적 제거됨: {enemy?.name ?? "null"}");
            #endif
            autoCombat.OnEnemyLost(enemy);
        }
    }
    
    public HashSet<GameObject> GetDetectedEnemies()
    {
        // 새로운 HashSet을 반환하여 원본 수정 방지
        return new HashSet<GameObject>(detectedEnemies);
    }
    
    public GameObject GetNearestEnemy(Vector3 position)
    {
        if (detectedEnemies.Count == 0) return null;
        
        GameObject nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (var enemy in detectedEnemies)
        {
            if (enemy == null) continue;
            
            float distance = Vector3.Distance(position, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }
        
        return nearest;
    }

    public float DetectionRadius => detectionRadius;
} 