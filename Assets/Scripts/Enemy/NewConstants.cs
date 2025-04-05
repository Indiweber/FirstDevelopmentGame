using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enemy
{
    public static class NewConstants
    {
        // 프레임 주기 상수 (기본값 = 3프레임)
        public enum UpdateFrequency
        {
            EveryFrame = 1,         // 매 프레임 (1/60초)
            EveryTwoFrames = 2,     // 2프레임마다 (1/30초)
            EveryThreeFrames = 3,   // 3프레임마다 (1/20초) - 기본값
            EveryFourFrames = 4,    // 4프레임마다 (1/15초)
            EverySixFrames = 6,     // 6프레임마다 (1/10초)
            EveryTenFrames = 10,    // 10프레임마다 (1/6초)
            EveryThirtyFrames = 30  // 30프레임마다 (1/2초)
        }

        // 기본 업데이트 주기 (3프레임)
        public const UpdateFrequency DefaultUpdateFrequency = UpdateFrequency.EveryThreeFrames;

        // 거리에 따른 업데이트 주기 설정
        public const float CloseDistance = 5f;       // 가까운 거리 (매 3-4프레임 업데이트)
        public const float MediumDistance = 10f;     // 중간 거리 (매 4프레임 업데이트)
        public const float FarDistance = 20f;        // 먼 거리 (매 6프레임 업데이트)
        public const float VeryFarDistance = 30f;    // 매우 먼 거리 (매 10프레임 업데이트)
        public const float ExtremeDistance = 50f;    // 극도로 먼 거리 (매 30프레임 업데이트)

        // 거리별 업데이트 주기
        public static UpdateFrequency GetUpdateFrequencyByDistance(float distance)
        {
            if (distance <= CloseDistance)
                return UpdateFrequency.EveryThreeFrames;
            else if (distance <= MediumDistance)
                return UpdateFrequency.EveryFourFrames;
            else if (distance <= FarDistance)
                return UpdateFrequency.EverySixFrames;
            else if (distance <= VeryFarDistance)
                return UpdateFrequency.EveryTenFrames;
            else
                return UpdateFrequency.EveryThirtyFrames;
        }

        // AI 상태 타입
        public enum EnemyState
        {
            Idle,       // 대기 상태
            Patrol,     // 순찰 상태 
            Chase,      // 추적 상태
            Attack,     // 공격 상태
            Flee,       // 도망 상태
            Stunned,    // 기절 상태
            Dead        // 죽음 상태
        }

        // 적 AI 설정
        public const float DetectionRadius = 10f;     // 적 감지 범위 ( 플레이어를 감지 )
        public const float AttackRadius = 3f;         // 근거리 공격 범위
        
        // 적 움직임 설정
        public const float WalkSpeed = 1f;            // 걷기 속도
        public const float RunSpeed = 3f;             // 달리기 속도
        public const float StoppingDistance = 3.5f;   // 정지 거리
        public const float AttackCooldown = 1.5f;     // 공격 쿨다운

        // 레이캐스트 설정
        public const int MaxRaycastHits = 5;          // NonAlloc 레이캐스트 최대 히트 수
        public const float RaycastDistance = 15f;     // 레이캐스트 최대 거리
        public const int ObstacleDetectionRays = 5;   // 장애물 감지용 레이 개수
        public const float RaySpread = 30f;           // 레이 퍼짐 각도
        public static readonly LayerMask ObstacleLayer = 1 << 8; // 장애물 레이어 (Layer 8)

        // Object Pooling 설정
        public const int DefaultPoolSize = 20;        // 기본 풀 크기
        public const int MaxPoolSize = 50;            // 최대 풀 크기
        public const int PrewarmCount = 5;            // 미리 생성할 오브젝트 수
        public const float PoolExpandThreshold = 0.8f; // 풀 확장 임계값 (80%)
        public const int PoolExpandAmount = 5;        // 풀 확장 시 추가 오브젝트 수
        
        // 성능 최적화 설정
        public const int FrameSkipThreshold = 50;     // 프레임 스킵 임계값 (50개 이상 적일 때)
        public const float LODDistance = 25f;         // LOD 전환 거리
        public const float CullingDistance = 40f;     // 컬링 거리

        // 애니메이션 파라미터 이름
        public const string ANIM_WALK = "Walk";
        public const string ANIM_ATTACK = "Attack";
    }
}
