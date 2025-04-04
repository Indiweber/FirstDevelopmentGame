// #define DEBUG_COMPONENT_NOT_FOUND
// #define DEBUG_ENEMY_DETECTION
// #define DEBUG_ENEMY_SEARCH
// #define DEBUG_LAYER_SEARCH

/* 디버그 정의
 * DEBUG_COMPONENT_NOT_FOUND: 필수 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_ENEMY_DETECTION: 적 감지 관련 디버그 정보를 출력
 * DEBUG_ENEMY_SEARCH: 적 탐색 관련 디버그 정보를 출력
 * DEBUG_LAYER_SEARCH: 레이어 기반 탐색 관련 디버그 정보를 출력
 */

using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(SphereCollider))]
public class EnemyDetector : MonoBehaviour
{
    [Tooltip("이동 및 전투 관련 설정이 포함된 설정 파일")]
    [SerializeField] private MovementSettings settings;

    [Tooltip("적 감지에 사용할 레이어 (Enemy 레이어 선택)")]
    [SerializeField] private LayerMask enemyLayer;
    
    [Tooltip("한 번에 감지할 수 있는 최대 적 수")]
    [SerializeField] private int maxDetectableEnemies = 20;
    
    [Tooltip("적 감지 간격 (초)")]
    [SerializeField] private float detectionInterval = 1.5f;

    // 디버그 옵션
    [SerializeField] private bool debugDetection = false;

    private readonly Collider[] detectedColliders = new Collider[20]; // 버퍼 크기 설정
    private Transform nearestEnemy;
    private float detectionTimer = 0f;
    private bool isActivelySearching = false;
    private int lastEnemyInstanceID = -1; // 마지막으로 감지된 적의 인스턴스 ID
    private float lastDetectionTime = 0f;
    private const float MIN_DETECTION_INTERVAL = 1.0f; // 최소 탐지 간격 증가
    
    // 개선된 캐싱 시스템
    [SerializeField] private float maxCacheTime = 5.0f;
    [SerializeField] private float cacheRefreshThreshold = 0.5f;
    
    private Transform cachedEnemy = null;
    private float cacheTimer = 0f;
    private Dictionary<Transform, float> enemyDistances = new Dictionary<Transform, float>();
    
    // 안정성을 위한 가중치
    private Dictionary<Transform, float> enemyWeights = new Dictionary<Transform, float>();
    private const float WEIGHT_INCREASE = 0.5f;
    private const float WEIGHT_DECREASE = 0.2f;
    private const float WEIGHT_THRESHOLD = 1.0f;

    private SphereCollider detectionCollider;
    private HashSet<GameObject> detectedEnemies = new HashSet<GameObject>();
    private AutoCombat autoCombat;

    [SerializeField] private float detectionRadius = 10f; // 이 필드 추가

    [SerializeField] private bool debugEnabled = false;

    public Transform NearestEnemy => nearestEnemy;
    public bool IsActivelySearching => isActivelySearching;
    
    public void SetActiveSearch(bool active)
    {
        if (isActivelySearching == active) return; // 이미 같은 상태면 아무것도 하지 않음
        
        isActivelySearching = active;
        if (active)
        {
            // 활성화될 때 즉시 감지 시작
            detectionTimer = detectionInterval;
            if (debugDetection)
            {
                Debug.Log("적 탐지 활성화");
            }
        }
        else
        {
            // 적 탐지 비활성화시 기존 타겟 유지
            if (nearestEnemy != null)
            {
                cachedEnemy = nearestEnemy;
                cacheTimer = 0f;
            }
            
            if (debugDetection)
            {
                Debug.Log("적 탐지 비활성화");
            }
        }
    }

    private void Awake()
    {
        detectionCollider = GetComponent<SphereCollider>();
        detectionCollider.radius = detectionRadius;
        detectionCollider.isTrigger = true;
        
        autoCombat = GetComponentInParent<AutoCombat>();
        if (autoCombat == null)
        {
            #if DEBUG_COMPONENT_NOT_FOUND
            Debug.LogError("EnemyDetector: AutoCombat 컴포넌트를 찾을 수 없습니다!");
            #endif
        }
        
        if (settings == null)
        {
            var playerMovement = GetComponent<CharacterMovement>();
            if (playerMovement != null)
            {
                settings = playerMovement.Settings;
            }
        }
        
        // Debug.Log($"EnemyDetector 시작: 레이어 마스크 = {enemyLayer.value}, 서치 반경 = {settings?.searchRadius}");
        lastDetectionTime = -MIN_DETECTION_INTERVAL; // 시작 시 즉시 탐지 가능하도록 설정
    }

    private void Update()
    {
        detectionTimer += Time.deltaTime;
        
        // 캐시된 적이 있는 경우 타이머 업데이트
        if (cachedEnemy != null)
        {
            cacheTimer += Time.deltaTime;
            
            // 캐시 만료 체크
            if (cacheTimer >= maxCacheTime)
            {
                if (debugDetection)
                {
                    Debug.Log("적 캐시 만료됨, 재탐색 시작");
                }
                ResetCache();
            }
            else if (detectionTimer >= cacheRefreshThreshold)
            {
                // 캐시된 적이 유효한지 확인
                if (!IsEnemyValid(cachedEnemy))
                {
                    if (debugDetection)
                    {
                        Debug.Log("캐시된 적이 더 이상 유효하지 않음, 재탐색 시작");
                    }
                    ResetCache();
                }
                else
                {
                    // 거리 업데이트
                    float distance = Vector3.Distance(transform.position, cachedEnemy.position);
                    enemyDistances[cachedEnemy] = distance;
                    
                    if (debugDetection && detectionTimer >= detectionInterval)
                    {
                        Debug.Log($"캐시된 적과의 거리 업데이트: {distance:F2}m");
                        detectionTimer = 0f;
                    }
                }
            }
        }
        
        // 탐지 간격이 충족되면 적 탐지 실행
        if (detectionTimer >= detectionInterval || (Time.time - lastDetectionTime >= MIN_DETECTION_INTERVAL && cachedEnemy == null))
        {
            DetectEnemies();
            detectionTimer = 0f;
            lastDetectionTime = Time.time;
        }
    }

    public void DetectEnemies()
    {
        // 캐시된 적이 있고 유효한 경우 계속 사용
        if (cachedEnemy != null && IsEnemyValid(cachedEnemy))
        {
            // 가중치 유지
            if (enemyWeights.ContainsKey(cachedEnemy))
            {
                enemyWeights[cachedEnemy] = Mathf.Min(enemyWeights[cachedEnemy] + WEIGHT_INCREASE, 3.0f);
            }
            return;
        }
        
        // 주변 적 검색
        Collider[] colliders = Physics.OverlapSphere(transform.position, settings.searchRadius, enemyLayer);
        
        if (colliders.Length == 0)
        {
            if (debugDetection)
            {
                Debug.Log("탐지된 적이 없습니다");
            }
            return;
        }
        
        // 적 거리 및 가중치 업데이트
        UpdateEnemyData(colliders);
        
        // 가중치 기반 가장 적합한 적 선택
        Transform bestEnemy = GetBestEnemy();
        
        if (bestEnemy != null)
        {
            cachedEnemy = bestEnemy;
            cacheTimer = 0f;
            
            if (debugDetection)
            {
                Debug.Log($"새 타겟 설정: {cachedEnemy.name}, 거리: {enemyDistances[cachedEnemy]:F2}m, 가중치: {enemyWeights[cachedEnemy]:F2}");
            }
        }
    }
    
    private void UpdateEnemyData(Collider[] colliders)
    {
        // 임시 리스트에 현재 키들을 복사
        var currentEnemies = new List<Transform>(enemyWeights.Keys);
        var enemiestoRemove = new List<Transform>();
        
        // 먼저 제거할 적들을 찾음
        foreach (var enemy in currentEnemies)
        {
            if (!IsEnemyValid(enemy))
            {
                enemiestoRemove.Add(enemy);
                continue;
            }
            
            // 가중치 감소는 별도 처리
            float currentWeight = enemyWeights[enemy];
            enemyWeights[enemy] = Mathf.Max(0, currentWeight - WEIGHT_DECREASE);
        }
        
        // 한번에 제거
        foreach (var enemy in enemiestoRemove)
        {
            enemyWeights.Remove(enemy);
            enemyDistances.Remove(enemy);
        }
        
        // 새로운 적들 처리
        foreach (Collider col in colliders)
        {
            if (!col.CompareTag("Enemy")) continue;
            
            Transform enemy = col.transform;
            float distance = Vector3.Distance(transform.position, enemy.position);
            
            enemyDistances[enemy] = distance;
            if (!enemyWeights.ContainsKey(enemy))
            {
                enemyWeights[enemy] = 0;
            }
            enemyWeights[enemy] = Mathf.Min(enemyWeights[enemy] + WEIGHT_INCREASE, 2f);
        }
    }
    
    private Transform GetBestEnemy()
    {
        Transform bestEnemy = null;
        float bestScore = float.MaxValue;
        
        foreach (var enemy in enemyDistances.Keys)
        {
            if (!IsEnemyValid(enemy)) continue;
            
            float distance = enemyDistances[enemy];
            float weight = enemyWeights.ContainsKey(enemy) ? enemyWeights[enemy] : 0f;
            
            // 가중치가 임계값 이상인 적만 고려
            if (weight < WEIGHT_THRESHOLD) continue;
            
            // 거리와 가중치를 결합한 점수 (거리가 짧고 가중치가 높을수록 좋음)
            float score = distance / (weight * 2);
            
            if (score < bestScore)
            {
                bestScore = score;
                bestEnemy = enemy;
            }
        }
        
        return bestEnemy;
    }
    
    private bool IsEnemyValid(Transform enemy)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy) return false;
        
        float distance = Vector3.Distance(transform.position, enemy.position);
        return distance <= settings.searchRadius;
    }
    
    private void ResetCache()
    {
        cachedEnemy = null;
        cacheTimer = 0f;
    }
    
    public Transform GetCurrentTarget()
    {
        // 캐시된 적 반환 (없으면 null)
        return cachedEnemy;
    }

    // 월드 내 모든 적 중 가장 가까운 적 찾기
    public Transform FindNearestEnemyInWorld()
    {
        if (Time.time - lastDetectionTime < MIN_DETECTION_INTERVAL) 
        {
            if (cachedEnemy != null && cachedEnemy.gameObject.activeInHierarchy)
            {
                return cachedEnemy;
            }
            if (nearestEnemy != null && nearestEnemy.gameObject.activeInHierarchy)
            {
                return nearestEnemy;
            }
        }
        
        lastDetectionTime = Time.time;
        
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        #if DEBUG_ENEMY_SEARCH
        Debug.Log($"Enemy 태그로 찾은 오브젝트 수: {allEnemies.Length}");
        #endif
        
        if (allEnemies.Length == 0)
        {
            allEnemies = FindAllGameObjectsWithLayer(LayerMask.NameToLayer("Enemy"));
            #if DEBUG_ENEMY_SEARCH
            Debug.Log($"Enemy 레이어로 찾은 오브젝트 수: {allEnemies.Length}");
            #endif
        }
        
        if (allEnemies.Length == 0)
        {
            #if DEBUG_ENEMY_SEARCH
            Debug.LogWarning("월드에 Enemy가 존재하지 않습니다!");
            #endif
            return null;
        }
            
        Transform closestEnemy = null;
        float closestDistance = float.MaxValue;
        
        foreach (GameObject enemy in allEnemies)
        {
            if (enemy == null || !enemy.activeInHierarchy) continue;
            
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }
        
        if (closestEnemy != null)
        {
            cachedEnemy = closestEnemy;
            cacheTimer = 0f;
            
            int enemyID = closestEnemy.GetInstanceID();
            
            if (enemyID != lastEnemyInstanceID)
            {
                lastEnemyInstanceID = enemyID;
                #if DEBUG_ENEMY_DETECTION
                Debug.Log($"가장 가까운 적 발견: {closestEnemy.name}, 거리: {closestDistance:F5}");
                #endif
            }
        }
        
        return closestEnemy;
    }
    
    private GameObject[] FindAllGameObjectsWithLayer(int layer)
    {
        #if DEBUG_LAYER_SEARCH
        Debug.Log($"Layer {layer}을 가진 오브젝트 검색 중...");
        #endif
        
        List<GameObject> result = new List<GameObject>();
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == layer)
            {
                result.Add(obj);
                #if DEBUG_LAYER_SEARCH
                Debug.Log($"Enemy 레이어 오브젝트 발견: {obj.name}");
                #endif
            }
        }
        
        return result.ToArray();
    }

    // 디버그용: 탐지된 모든 적 반환
    public List<Transform> GetAllDetectedEnemies()
    {
        List<Transform> enemies = new List<Transform>();
        int numColliders = Physics.OverlapSphereNonAlloc(
            transform.position,
            settings.searchRadius,
            detectedColliders,
            enemyLayer
        );

        for (int i = 0; i < numColliders; i++)
        {
            if (detectedColliders[i] == null) continue; // 널 체크 추가
            if (detectedColliders[i].gameObject != gameObject)
            {
                enemies.Add(detectedColliders[i].transform);
            }
        }

        return enemies;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (settings == null) return;
        
        // 감지 범위 시각화
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, settings.searchRadius);
        
        // 가장 가까운 적에 대한 선 표시
        if (nearestEnemy != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, nearestEnemy.position);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            detectedEnemies.Add(other.gameObject);
            if (debugEnabled)
            {
                Debug.Log($"적 감지됨: {other.gameObject.name}");
            }
            autoCombat?.OnEnemyDetected(other.gameObject);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            detectedEnemies.Remove(other.gameObject);
            if (debugEnabled)
            {
                Debug.Log($"적 감지 범위 이탈: {other.gameObject.name}");
            }
            autoCombat?.OnEnemyLost(other.gameObject);
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
                continue;
            }
            
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance > detectionRadius)
            {
                invalidEnemies.Add(enemy);
            }
        }
        
        foreach (var enemy in invalidEnemies)
        {
            detectedEnemies.Remove(enemy);
            if (debugEnabled)
            {
                Debug.Log($"유효하지 않은 적 제거됨: {enemy?.name ?? "null"}");
            }
            autoCombat?.OnEnemyLost(enemy);
        }
    }
    
    public HashSet<GameObject> GetDetectedEnemies()
    {
        return new HashSet<GameObject>(detectedEnemies);
    }
} 