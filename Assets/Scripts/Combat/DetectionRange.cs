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
            Debug.LogError("DetectionRange: AutoCombat 컴포넌트를 찾을 수 없습니다!");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            detectedEnemies.Add(other.gameObject);
            autoCombat.OnEnemyDetected(other.gameObject);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            detectedEnemies.Remove(other.gameObject);
            autoCombat.OnEnemyLost(other.gameObject);
            
            if (autoCombat.Animator != null && detectedEnemies.Count == 0)
            {
                autoCombat.Animator.SetBool("Walk", false);
                autoCombat.Animator.SetBool("Idle", true);
                
                if (autoCombat.DrawDebugInfo)
                {
                    Debug.Log("[DetectionRange] 모든 적이 감지 범위를 벗어남 - Idle 상태로 전환");
                }
            }
        }
    }
    
    public void ValidateDetectedEnemies()
    {
        var validEnemies = new HashSet<GameObject>();
        var invalidEnemies = new List<GameObject>();
        
        // 먼저 유효한 적들을 찾음
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                validEnemies.Add(col.gameObject);
            }
        }
        
        // 유효하지 않은 적들을 찾음
        foreach (var enemy in detectedEnemies)
        {
            if (!validEnemies.Contains(enemy))
            {
                invalidEnemies.Add(enemy);
            }
        }
        
        // 유효하지 않은 적들 제거
        foreach (var enemy in invalidEnemies)
        {
            detectedEnemies.Remove(enemy);
            autoCombat.OnEnemyLost(enemy);
        }
        
        // 새로운 적들 추가
        foreach (var enemy in validEnemies)
        {
            if (!detectedEnemies.Contains(enemy))
            {
                detectedEnemies.Add(enemy);
                autoCombat.OnEnemyDetected(enemy);
            }
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