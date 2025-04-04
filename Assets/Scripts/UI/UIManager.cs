// #define DEBUG_COMPONENT_NOT_FOUND
// #define DEBUG_UI_STATE
// #define DEBUG_UI_INTERACTION

/* 디버그 정의
 * DEBUG_COMPONENT_NOT_FOUND: 필수 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_UI_STATE: UI 상태 변경 관련 디버그 정보를 출력
 * DEBUG_UI_INTERACTION: UI 상호작용 관련 디버그 정보를 출력
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private AutoButton autoButton;
    
    [Header("참조")]
    [SerializeField] private GameObject playerObject;
    
    [SerializeField] private Image healthBar;
    [SerializeField] private TextMeshProUGUI scoreText;

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

        if (healthBar == null)
        {
            #if DEBUG_COMPONENT_NOT_FOUND
            Debug.LogError("체력바가 설정되지 않았습니다!");
            #endif
        }

        if (scoreText == null)
        {
            #if DEBUG_COMPONENT_NOT_FOUND
            Debug.LogError("점수 텍스트가 설정되지 않았습니다!");
            #endif
        }
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

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBar != null)
        {
            float healthPercentage = currentHealth / maxHealth;
            healthBar.fillAmount = healthPercentage;
            #if DEBUG_UI_STATE
            Debug.Log($"체력바 업데이트: {healthPercentage:P0}");
            #endif
        }
    }

    public void UpdateScore(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {newScore}";
            #if DEBUG_UI_STATE
            Debug.Log($"점수 업데이트: {newScore}");
            #endif
        }
    }

    public void OnButtonClick(string buttonName)
    {
        #if DEBUG_UI_INTERACTION
        Debug.Log($"버튼 클릭: {buttonName}");
        #endif
        // 버튼 클릭 처리 로직
    }
} 