using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private AutoButton autoButton;
    
    [Header("참조")]
    [SerializeField] private GameObject playerObject;
    
    private void Awake()
    {
        // 플레이어 오브젝트 자동 찾기
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
        }
        
        // Auto 버튼 초기화
        if (autoButton != null && playerObject != null)
        {
            // 플레이어 오브젝트 설정
            autoButton.SetPlayer(playerObject);
            Debug.Log("UIManager: 자동 버튼에 플레이어 오브젝트 설정 완료");
        }
    }
    
    private void Start()
    {
        // 시작 시 자동 모드를 명시적으로 비활성화
        EnableAutoModeOnStart(false);
    }
    
    // 자동 모드 활성화/비활성화
    public void EnableAutoModeOnStart(bool enable)
    {
        if (autoButton != null)
        {
            Debug.Log($"UIManager: 자동 모드 초기화 - {(enable ? "활성화" : "비활성화")}");
            
            // 복원된 SetAutoMode 메서드 사용
            autoButton.SetAutoMode(enable);
            
            // 설정 후 상태 확인
            bool currentState = autoButton.GetAutoModeState();
            Debug.Log($"UIManager: 자동 모드 설정 결과 - {currentState}");
        }
        else
        {
            Debug.LogError("UIManager: AutoButton을 찾을 수 없습니다!");
        }
    }
} 