using UnityEngine;
using System.Collections.Generic;

public class AttackRange : MonoBehaviour
{
    private HashSet<GameObject> enemiesInRange = new HashSet<GameObject>();
    private AutoCombat autoCombat;
    
    [SerializeField] private float attackRadius = 2f;
    [SerializeField] private LayerMask enemyLayer;
    
    private void Awake()
    {
        autoCombat = GetComponentInParent<AutoCombat>();
        if (autoCombat == null)
        {
            Debug.LogError("AttackRange: AutoCombat 컴포넌트를 찾을 수 없습니다!");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesInRange.Add(other.gameObject);
            autoCombat.OnEnemyEnteredAttackRange(other.gameObject);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemiesInRange.Remove(other.gameObject);
            autoCombat.OnEnemyExitedAttackRange(other.gameObject);
        }
    }
    
    public void ValidateEnemiesInRange()
    {
        HashSet<GameObject> validEnemies = new HashSet<GameObject>();
        Collider[] colliders = Physics.OverlapSphere(transform.position, attackRadius, enemyLayer);
        
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                validEnemies.Add(col.gameObject);
            }
        }
        
        // 유효하지 않은 적 제거
        enemiesInRange.RemoveWhere(enemy => !validEnemies.Contains(enemy));
        
        // 새로운 적 추가
        foreach (GameObject enemy in validEnemies)
        {
            if (!enemiesInRange.Contains(enemy))
            {
                enemiesInRange.Add(enemy);
                autoCombat.OnEnemyEnteredAttackRange(enemy);
            }
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