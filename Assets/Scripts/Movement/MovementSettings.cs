// #define DEBUG_SETTINGS_CHANGE

/* 디버그 정의
 * DEBUG_SETTINGS_CHANGE: 이동 설정 변경 관련 디버그 정보를 출력
 */

using UnityEngine;

[CreateAssetMenu(fileName = "MovementSettings", menuName = "Game/Movement Settings")]
public class MovementSettings : ScriptableObject
{
    [Header("기본 이동 설정")]
    [Range(0f, 20f)]
    [Tooltip("캐릭터의 기본 이동 속도")]
    public float moveSpeed = 5f;

    [Header("전투 설정")]
    [Range(0f, 50f)]
    [Tooltip("주변 적을 감지할 수 있는 최대 거리")]
    public float searchRadius = 10f;

    [Range(0f, 30f)]
    [Tooltip("적을 향해 돌진하는 속도")]
    public float dashSpeed = 10f;

    [Range(0f, 5f)]
    [Tooltip("적과 유지할 최소 거리")]
    public float minEnemyDistance = 1f;

    [Tooltip("디버그 시각화에 사용될 탐지 범위의 색상")]
    public Color searchRadiusColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);

    public void SetMovementSpeed(float value)
    {
        moveSpeed = value;
        #if DEBUG_SETTINGS_CHANGE
        Debug.Log($"이동 속도 변경: {value}");
        #endif
    }

    public void SetRotationSpeed(float value)
    {
        // This method is not used in the original file or the new file
        // It's kept here as it was in the original file
    }

    public void SetJumpForce(float value)
    {
        // This method is not used in the original file or the new file
        // It's kept here as it was in the original file
    }
} 