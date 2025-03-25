using UnityEngine;
using System.Collections.Generic;
using System.Collections;

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

    private void Start()
    {
        if (settings == null)
        {
            var playerMovement = GetComponent<CharacterMovement>();
            if (playerMovement != null)
            {
                settings = playerMovement.Settings;
            }
        }
        
        Debug.Log($"EnemyDetector 시작: 레이어 마스크 = {enemyLayer.value}, 서치 반경 = {settings?.searchRadius}");
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
        // 기존 가중치 감소
        List<Transform> keysToRemove = new List<Transform>();
        foreach (var enemy in enemyWeights.Keys)
        {
            if (!IsEnemyValid(enemy))
            {
                keysToRemove.Add(enemy);
                continue;
            }
            
            enemyWeights[enemy] = Mathf.Max(0, enemyWeights[enemy] - WEIGHT_DECREASE);
        }
        
        // 유효하지 않은 적 제거
        foreach (var key in keysToRemove)
        {
            enemyWeights.Remove(key);
            enemyDistances.Remove(key);
        }
        
        // 새 적 거리 계산 및 가중치 업데이트
        foreach (var collider in colliders)
        {
            if (collider.transform == null) continue;
            
            Transform enemy = collider.transform;
            float distance = Vector3.Distance(transform.position, enemy.position);
            
            enemyDistances[enemy] = distance;
            
            // 새 적이거나 가중치가 낮은 경우 가중치 증가
            if (!enemyWeights.ContainsKey(enemy))
            {
                enemyWeights[enemy] = WEIGHT_THRESHOLD;
            }
            else
            {
                enemyWeights[enemy] = Mathf.Min(enemyWeights[enemy] + WEIGHT_INCREASE, 3.0f);
            }
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
        // 적이 존재하는지, 활성화된 상태인지 확인
        return enemy != null && enemy.gameObject.activeInHierarchy;
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
        // 최소 탐지 간격 확인
        if (Time.time - lastDetectionTime < MIN_DETECTION_INTERVAL) 
        {
            // 캐시된 적 반환
            if (cachedEnemy != null && cachedEnemy.gameObject.activeInHierarchy)
            {
                return cachedEnemy;
            }
            // 현재 가장 가까운 적 반환
            if (nearestEnemy != null && nearestEnemy.gameObject.activeInHierarchy)
            {
                return nearestEnemy;
            }
        }
        
        lastDetectionTime = Time.time;
        
        // 씬에서 Enemy 레이어를 가진 모든 객체 찾기
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        if (debugDetection)
        {
            Debug.Log($"Enemy 태그로 찾은 오브젝트 수: {allEnemies.Length}");
        }
        
        if (allEnemies.Length == 0)
        {
            // Enemy 태그가 없을 경우 레이어를 직접 확인
            allEnemies = FindAllGameObjectsWithLayer(LayerMask.NameToLayer("Enemy"));
            if (debugDetection)
            {
                Debug.Log($"Enemy 레이어로 찾은 오브젝트 수: {allEnemies.Length}");
            }
        }
        
        if (allEnemies.Length == 0)
        {
            Debug.LogWarning("월드에 Enemy가 존재하지 않습니다!");
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
            // 캐시 업데이트
            cachedEnemy = closestEnemy;
            cacheTimer = 0f;
            
            // 적의 인스턴스 ID 저장
            int enemyID = closestEnemy.GetInstanceID();
            
            // 새로운 적인 경우에만 로그 출력
            if (enemyID != lastEnemyInstanceID)
            {
                lastEnemyInstanceID = enemyID;
                Debug.Log($"가장 가까운 적 발견: {closestEnemy.name}, 거리: {closestDistance:F5}");
            }
        }
        
        return closestEnemy;
    }
    
    // 특정 레이어를 가진 모든 오브젝트 찾기
    private GameObject[] FindAllGameObjectsWithLayer(int layer)
    {
        if (debugDetection)
        {
            Debug.Log($"Layer {layer}을 가진 오브젝트 검색 중...");
        }
        
        List<GameObject> result = new List<GameObject>();
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == layer)
            {
                result.Add(obj);
                if (debugDetection)
                {
                    Debug.Log($"Enemy 레이어 오브젝트 발견: {obj.name}");
                }
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
} 