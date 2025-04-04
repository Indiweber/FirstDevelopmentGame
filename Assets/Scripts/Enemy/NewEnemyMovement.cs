#define DEBUG_COMPONENT_NOT_FOUND
#define DEBUG_MOVEMENT_STATE
#define DEBUG_ATTACK_STATE
#define DEBUG_PATROL_STATE
#define DEBUG_STUN_STATE
#define DEBUG_TRIGGER_EVENT

/* 디버그 정의
 * DEBUG_COMPONENT_NOT_FOUND: 필수 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_MOVEMENT_STATE: 이동 상태 변경 및 업데이트 관련 디버그 정보를 출력
 * DEBUG_ATTACK_STATE: 공격 상태 및 행동 관련 디버그 정보를 출력
 * DEBUG_PATROL_STATE: 순찰 상태 및 동작 관련 디버그 정보를 출력
 * DEBUG_STUN_STATE: 스턴 상태 관련 디버그 정보를 출력
 * DEBUG_TRIGGER_EVENT: 트리거 이벤트 관련 디버그 정보를 출력
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NewEnemyMovement : MonoBehaviour
    {
        // NavMesh 컴포넌트
        private NavMeshAgent navAgent;
        
        // 플레이어 참조
        private Transform playerTransform;

        // 공격 범위 콜라이더
        [SerializeField] private SphereCollider attackRangeCollider;
        
        // 현재 적 상태
        private NewConstants.EnemyState currentState = NewConstants.EnemyState.Chase;
        
        // 순찰 관련 변수
        private Vector3[] patrolPoints;
        private int currentPatrolIndex = 0;
        private float patrolWaitTimer = 0f;
        private bool isWaiting = false;
        
        // 업데이트 최적화 변수
        private int frameCounter = 0;
        private NewConstants.UpdateFrequency currentUpdateFrequency;
        private float distanceToPlayer = float.MaxValue;
        
        // 타이머 변수
        private float lastAttackTime = 0f;
        
        // 초기화
        void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            
            // NavMeshAgent 회전 비활성화
            navAgent.updateRotation = false;
            navAgent.updateUpAxis = false;

            
            // AttackRange 콜라이더 설정
            if (attackRangeCollider == null)
            {
                attackRangeCollider = gameObject.AddComponent<SphereCollider>();
                attackRangeCollider.radius = NewConstants.AttackRadius;
                attackRangeCollider.isTrigger = true;
            }
            
            // NavMeshAgent 초기 설정
            navAgent.speed = NewConstants.RunSpeed;
            navAgent.stoppingDistance = NewConstants.StoppingDistance;
            navAgent.acceleration = float.MaxValue; // 즉시 최대 속도로 설정
            navAgent.angularSpeed = 0f; // 회전 속도 제거
            
            // 초기 위치 저장
            fixedY = transform.position.y;
        }
        
        private float fixedY;
        private Animator animator;

        void LateUpdate()
        {
            // Y축과 고정
            if (transform.position.y != fixedY)
            {
                transform.position = new Vector3(transform.position.x, fixedY, transform.position.z);
            }
        }
        
        void Start()
        {
            // 초기 Y축 위치만 저장
            fixedY = transform.position.y;
            
            // 플레이어 찾기
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            
            // AttackRange 콜라이더 찾기
            attackRangeCollider = GetComponentInChildren<SphereCollider>();
            
            if (navAgent != null)
            {
                navAgent.speed = NewConstants.RunSpeed;
                navAgent.stoppingDistance = NewConstants.StoppingDistance;
                navAgent.acceleration = NewConstants.RunSpeed / NewConstants.AccelerationTime;
            }
            
            // 초기 업데이트 주기 설정
            UpdateDistanceToPlayer();
            currentUpdateFrequency = NewConstants.GetUpdateFrequencyByDistance(distanceToPlayer);
        }
        
        void Update()
        {
            // 프레임 카운터 증가
            frameCounter++;
            
            // 현재 업데이트 주기에 따라 업데이트 실행
            if (frameCounter >= (int)currentUpdateFrequency)
            {
                // 실제 업데이트 로직 실행
                PerformUpdate();
                
                // 프레임 카운터 리셋
                frameCounter = 0;
            }

            // Y축과 Z축 고정
            if (transform.position.y != fixedY)
            {
                transform.position = new Vector3(transform.position.x, fixedY, transform.position.z);
            }
        }
        
        // 실제 업데이트 로직
        private void PerformUpdate()
        {
            if (playerTransform == null) return;
            
            // 거리 업데이트 및 업데이트 주기 재조정
            UpdateDistanceToPlayer();
            currentUpdateFrequency = NewConstants.GetUpdateFrequencyByDistance(distanceToPlayer);
            
            // 상태에 따른 행동 업데이트
            switch (currentState)
            {
                case NewConstants.EnemyState.Chase:
                    UpdateChaseBehavior();
                    UpdateRotationBasedOnMovement();
                    break;
                    
                case NewConstants.EnemyState.Attack:
                    UpdateAttackBehavior();
                    if (playerTransform != null)
                    {
                        LookAtTarget(playerTransform.position);
                    }
                    break;
                    
                case NewConstants.EnemyState.Stunned:
                    // 스턴 상태에서는 회전하지 않음
                    break;
            }
            
            // 상태 전환 체크
            CheckStateTransitions();

            // 애니메이션 상태 업데이트
            UpdateAnimationState();
        }
        
        private void UpdateAnimationState()
        {
            if (animator != null)
            {
                // Walk 애니메이션
                animator.SetBool(NewConstants.ANIM_WALK, currentState == NewConstants.EnemyState.Chase);
                
                // Attack 애니메이션
                animator.SetBool(NewConstants.ANIM_ATTACK, currentState == NewConstants.EnemyState.Attack);
            }
        }
        
        // 플레이어와의 거리 업데이트
        private void UpdateDistanceToPlayer()
        {
            if (playerTransform != null)
            {
                Vector3 playerPos = playerTransform.position;
                Vector3 enemyPos = transform.position;
                // Y축을 제외한 거리 계산
                playerPos.y = enemyPos.y = 0;
                distanceToPlayer = Vector3.Distance(enemyPos, playerPos);
            }
        }
        
        // 상태 기반 초기화
        private void InitializeStateBasedProperties()
        {
            switch (currentState)
            {
                case NewConstants.EnemyState.Chase:
                    navAgent.isStopped = false;
                    navAgent.speed = NewConstants.RunSpeed;
                    break;
                    
                case NewConstants.EnemyState.Attack:
                    navAgent.isStopped = true;
                    break;
                    
                case NewConstants.EnemyState.Stunned:
                    navAgent.isStopped = true;
                    break;
            }
        }
        
        #region State Behaviors
        
        // 추적 상태 업데이트
        private void UpdateChaseBehavior()
        {
            if (playerTransform != null)
            {
                Vector3 targetPosition = playerTransform.position;
                targetPosition.y = fixedY;  // Y축만 고정
                
                // NavMeshAgent로 이동
                navAgent.SetDestination(targetPosition);
                
                #if DEBUG_MOVEMENT_STATE
                Debug.Log($"[Enemy] 추적 중 - 목표 위치: {targetPosition}, 거리: {distanceToPlayer}");
                #endif
            }
        }
        
        // 공격 상태 업데이트
        private void UpdateAttackBehavior()
        {
            if (playerTransform == null) return;
            
            // 플레이어를 바라봄
            LookAtTarget(playerTransform.position);
            
            // 공격 쿨다운 체크
            if (Time.time - lastAttackTime >= NewConstants.AttackCooldown)
            {
                PerformAttack();
                lastAttackTime = Time.time;
                
                #if DEBUG_ATTACK_STATE
                Debug.Log($"[Enemy] 공격 수행 - 쿨다운: {NewConstants.AttackCooldown}초");
                #endif
            }
        }
        
        #endregion
        
        #region State Transitions
        
        // 상태 전환 체크
        private void CheckStateTransitions()
        {
            NewConstants.EnemyState newState = currentState;
            
            switch (currentState)
            {
                case NewConstants.EnemyState.Chase:
                    // 플레이어가 공격 범위 안에 있으면 공격
                    if (distanceToPlayer <= NewConstants.AttackRadius)
                    {
                        newState = NewConstants.EnemyState.Attack;
                    }
                    break;
                    
                case NewConstants.EnemyState.Attack:
                    // 플레이어가 공격 범위 밖으로 나가면 추적
                    if (distanceToPlayer > NewConstants.AttackRadius * 1.2f)
                    {
                        newState = NewConstants.EnemyState.Chase;
                    }
                    break;
                    
                case NewConstants.EnemyState.Stunned:
                    // 스턴 상태는 외부에서 해제
                    break;
            }
            
            // 상태가 변경되면 상태 기반 초기화 실행
            if (newState != currentState)
            {
                currentState = newState;
                InitializeStateBasedProperties();
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        // 패트롤 포인트 생성
        private void GeneratePatrolPoints()
        {
            patrolPoints = new Vector3[NewConstants.MaxPatrolPoints];
            
            Vector3 center = transform.position;
            
            for (int i = 0; i < NewConstants.MaxPatrolPoints; i++)
            {
                // 랜덤 방향과 거리
                Vector2 randomCircle = Random.insideUnitCircle * NewConstants.PatrolRadius;
                Vector3 randomPos = center + new Vector3(randomCircle.x, 0, randomCircle.y);
                
                // NavMesh 내에서 유효한 위치 찾기
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPos, out hit, NewConstants.PatrolRadius, NavMesh.AllAreas))
                {
                    patrolPoints[i] = hit.position;
                }
                else
                {
                    patrolPoints[i] = center;
                }
            }
        }
        
        // 다음 패트롤 포인트로 이동
        private void SetDestinationToNextPatrolPoint()
        {
            if (patrolPoints.Length == 0) return;
            
            navAgent.SetDestination(patrolPoints[currentPatrolIndex]);
            
            // 다음 인덱스로 이동
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
        
        // 타겟 방향으로 2D 회전 (좌/우만)
        private void LookAtTarget(Vector3 target)
        {
            // 현재 위치와 타겟의 X 좌표 비교
            bool shouldFaceLeft = (target.x < transform.position.x);
            
            // 0도 = 오른쪽, 180도 = 왼쪽
            Vector3 newRotation = new Vector3(0, shouldFaceLeft ? 180f : 0f, 0);
            transform.eulerAngles = newRotation;
        }
        
        // 이동 방향에 따라 회전
        private void UpdateRotationBasedOnMovement()
        {
            if (navAgent.velocity.magnitude > 0.1f)
            {
                bool isMovingLeft = (navAgent.velocity.x < 0);
                transform.eulerAngles = new Vector3(0, isMovingLeft ? 180f : 0f, 0);
            }
        }
        
        // 공격 수행
        private void PerformAttack()
        {
            Debug.Log($"Enemy attacking player");
        }
        
        // 스턴 적용 (외부에서 호출)
        public void ApplyStun(float duration)
        {
            if (currentState != NewConstants.EnemyState.Stunned)
            {
                StartCoroutine(StunCoroutine(duration));
                
                #if DEBUG_STUN_STATE
                Debug.Log($"[Enemy] 스턴 적용 - 지속시간: {duration}초");
                #endif
            }
        }
        
        // 스턴 코루틴
        private IEnumerator StunCoroutine(float duration)
        {
            // 이전 상태 저장
            NewConstants.EnemyState previousState = currentState;
            
            // 스턴 상태로 변경
            currentState = NewConstants.EnemyState.Stunned;
            navAgent.isStopped = true;
            
            #if DEBUG_STUN_STATE
            Debug.Log($"[Enemy] 스턴 시작 - 이전 상태: {previousState}");
            #endif
            
            yield return new WaitForSeconds(duration);
            
            // 스턴 해제
            currentState = previousState;
            navAgent.isStopped = false;
            
            #if DEBUG_STUN_STATE
            Debug.Log($"[Enemy] 스턴 해제 - 복귀 상태: {previousState}");
            #endif
        }
        
        // 충돌 감지
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                currentState = NewConstants.EnemyState.Attack;
                
                #if DEBUG_TRIGGER_EVENT
                Debug.Log($"[Enemy] 플레이어 감지 - 공격 상태로 전환");
                #endif
            }
        }
        
        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                currentState = NewConstants.EnemyState.Chase;
                
                #if DEBUG_TRIGGER_EVENT
                Debug.Log($"[Enemy] 플레이어 이탈 - 추적 상태로 전환");
                #endif
            }
        }
        
        #endregion
        
        // Gizmos 그리기 (디버깅용)
        void OnDrawGizmosSelected()
        {
            // 감지 범위 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, NewConstants.DetectionRadius);
            
            // 공격 범위 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, NewConstants.AttackRadius);
            
            // 패트롤 포인트 표시
            if (patrolPoints != null)
            {
                Gizmos.color = Color.blue;
                foreach (Vector3 point in patrolPoints)
                {
                    Gizmos.DrawSphere(point, 0.3f);
                }
                
                // 패트롤 경로 연결
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    Gizmos.DrawLine(patrolPoints[i], patrolPoints[(i + 1) % patrolPoints.Length]);
                }
            }
        }
    }
}
