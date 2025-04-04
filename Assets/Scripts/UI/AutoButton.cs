// #define DEBUG_BUTTON_IMAGE_NOT_FOUND
// #define DEBUG_PLAYER_OBJECT_NOT_FOUND
// #define DEBUG_NOT_RESET_AUTO_MODE
// #define DEBUG_COMPONENT_NOT_FOUND
// #define DEBUG_AUTO_MODE_CHANGE
// #define DEBUG_PLAYER_SET
// #define DEBUG_AUTO_MODE_TOGGLE
// #define DEBUG_AUTO_MODE_MISMATCH
// #define DEBUG_FORCE_DISABLE

/* 디버그 정의
 * DEBUG_BUTTON_IMAGE_NOT_FOUND: 버튼 이미지 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_PLAYER_OBJECT_NOT_FOUND: 플레이어 오브젝트를 찾지 못했을 때 에러를 출력
 * DEBUG_NOT_RESET_AUTO_MODE: 자동 모드 초기화 실패 시 경고를 출력
 * DEBUG_COMPONENT_NOT_FOUND: 필수 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_AUTO_MODE_CHANGE: 자동 모드 상태 변경 관련 디버그 정보를 출력
 * DEBUG_PLAYER_SET: 플레이어 오브젝트 설정 관련 디버그 정보를 출력
 * DEBUG_AUTO_MODE_TOGGLE: 자동 모드 토글 관련 디버그 정보를 출력
 * DEBUG_AUTO_MODE_MISMATCH: 자동 모드 설정 불일치 시 에러를 출력
 * DEBUG_FORCE_DISABLE: 자동 모드 강제 비활성화 관련 디버그 정보를 출력
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class AutoButton : MonoBehaviour
{
    [Header("버튼 설정")]
    [Tooltip("자동 모드 활성화 시 버튼 색상")]
    [SerializeField] private Color activeColor = new Color(0.2f, 0.8f, 0.2f);
    
    [Tooltip("자동 모드 비활성화 시 버튼 색상")]
    [SerializeField] private Color inactiveColor = new Color(0.8f, 0.2f, 0.2f);
    
    [Tooltip("버튼 텍스트 컴포넌트 (선택사항)")]
    [SerializeField] private TextMeshProUGUI buttonText;
    
    [Tooltip("플레이어 오브젝트")]
    [SerializeField] private GameObject playerObject;
    
    [SerializeField] private bool enableDebugLogs = true;  // 디버그 로그 기본 활성화
    
    // 컴포넌트 캐싱
    private Button button;
    private Image buttonImage;
    private InputManager inputManager;
    private AutoCombat autoCombat;
    
    // 현재 자동 모드 상태
    private bool isAutoModeActive = false;

    private void Awake()
    {
        // 컴포넌트 가져오기
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        
        if (buttonImage == null)
        {
            #if DEBUG_BUTTON_IMAGE_NOT_FOUND
            Debug.LogError("버튼 이미지 컴포넌트를 찾을 수 없습니다!");
            #endif
            return;
        }
        
        // 플레이어 오브젝트가 지정되지 않았다면 찾기
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                #if DEBUG_PLAYER_OBJECT_NOT_FOUND
                Debug.LogError("플레이어 오브젝트를 찾을 수 없습니다!");
                #endif
                return;
            }
        }
        
        // 버튼 클릭 이벤트 설정
        button.onClick.AddListener(ToggleAutoMode);
        
        // 초기 상태 설정
        isAutoModeActive = false;
        UpdateButtonVisual();
    }
    
    private void Start()
    {
        // 컴포넌트 초기화
        InitializeComponents();
        
        // 시작 시 자동 이동 모드 강제 비활성화
        if (inputManager != null && autoCombat != null)
        {
            ForceDisableAutoMode();
        }
        else
        {
            #if DEBUG_NOT_RESET_AUTO_MODE
            Debug.LogWarning("필수 컴포넌트가 없어 자동 모드를 초기화할 수 없습니다.");
            #endif
        }
    }

    private void Update()
    {
        // 현재 상태 확인 및 동기화
        if (autoCombat != null)
        {
            bool currentAutoState = autoCombat.IsAutoModeEnabled();
            if (isAutoModeActive != currentAutoState)
            {
                isAutoModeActive = currentAutoState;
                UpdateButtonVisual();
            }
        }
    }
    
    private void InitializeComponents()
    {
        if (playerObject != null)
        {
            // if (enableDebugLogs)
            // {
            //     Debug.Log($"플레이어 오브젝트 찾음: {playerObject.name}");
            // }
            
            // 플레이어 컴포넌트 가져오기
            inputManager = playerObject.GetComponent<InputManager>();
            autoCombat = playerObject.GetComponent<AutoCombat>();
            
            // InputManager가 없다면 자식이나 부모에서 찾기
            if (inputManager == null)
            {
                inputManager = playerObject.GetComponentInChildren<InputManager>();
                if (inputManager == null)
                {
                    inputManager = playerObject.GetComponentInParent<InputManager>();
                }
            }
            
            if (inputManager == null)
            {
                #if DEBUG_COMPONENT_NOT_FOUND
                Debug.LogError($"InputManager 컴포넌트를 찾을 수 없습니다! 플레이어: {playerObject.name}");
                #endif
            }
            else if (enableDebugLogs)
            {
                Debug.Log("InputManager 컴포넌트를 찾았습니다.");
            }
            
            if (autoCombat == null)
            {
                #if DEBUG_COMPONENT_NOT_FOUND
                Debug.LogError($"AutoCombat 컴포넌트를 찾을 수 없습니다! 플레이어: {playerObject.name}");
                #endif
            }
            else if (enableDebugLogs)
            {
                Debug.Log("AutoCombat 컴포넌트를 찾았습니다.");
            }
        }
    }
    
    // 플레이어 오브젝트 설정 메서드 (UIManager에서 사용)
    public void SetPlayer(GameObject player)
    {
        if (player != null)
        {
            playerObject = player;
            #if DEBUG_PLAYER_SET
            Debug.Log($"플레이어 오브젝트 설정: {player.name}");
            #endif
            
            InitializeComponents();
            
            // 설정 후 초기 상태 업데이트
            isAutoModeActive = false;
            UpdateAutoMode();
        }
        else
        {
            #if DEBUG_PLAYER_SET
            Debug.LogError("SetPlayer: 전달된 플레이어 오브젝트가 null입니다!");
            #endif
        }
    }
    
    public void ToggleAutoMode()
    {
        if (inputManager == null || autoCombat == null)
        {
            #if DEBUG_AUTO_MODE_TOGGLE
            Debug.LogError("필수 컴포넌트가 없어 자동 모드를 전환할 수 없습니다!");
            #endif
            return;
        }

        isAutoModeActive = !isAutoModeActive;
        #if DEBUG_AUTO_MODE_TOGGLE
        Debug.Log($"자동 모드 토글: {isAutoModeActive}");
        #endif
        
        UpdateAutoMode();
    }
    
    // 자동 모드 상태 직접 설정 (UIManager에서 사용)
    public void SetAutoMode(bool active)
    {
        if (inputManager == null || autoCombat == null)
        {
            #if DEBUG_AUTO_MODE_CHANGE
            Debug.LogError("필수 컴포넌트가 없어 자동 모드를 설정할 수 없습니다!");
            #endif
            
            // 컴포넌트 다시 찾기 시도
            if (playerObject != null)
            {
                inputManager = playerObject.GetComponent<InputManager>();
                autoCombat = playerObject.GetComponent<AutoCombat>();
                
                if (inputManager == null || autoCombat == null)
                {
                    #if DEBUG_COMPONENT_NOT_FOUND
                    Debug.LogError("컴포넌트를 찾을 수 없습니다. 자동 모드를 설정할 수 없습니다!");
                    #endif
                    return;
                }
            }
            else
            {
                return;
            }
        }
        
        isAutoModeActive = active;
        autoCombat.SetAutoMode(active);
        InputType newPriority = active ? InputType.Virtual : InputType.Joystick;
        inputManager.SetPriorityInputType(newPriority);
        UpdateButtonVisual();
        
        #if DEBUG_AUTO_MODE_CHANGE
        Debug.Log($"자동 모드 변경 완료: {active}, 우선순위: {newPriority}, AutoCombat 상태: {autoCombat.IsAutoModeEnabled()}");
        #endif
        
        if (active != autoCombat.IsAutoModeEnabled())
        {
            #if DEBUG_AUTO_MODE_MISMATCH
            Debug.LogError($"자동 모드 설정 불일치: 요청={active}, 실제={autoCombat.IsAutoModeEnabled()}");
            #endif
        }
    }
    
    // 현재 자동 모드 상태 반환 (UIManager에서 사용)
    public bool GetAutoModeState()
    {
        return isAutoModeActive;
    }
    
    private void UpdateAutoMode()
    {
        if (inputManager == null || autoCombat == null) return;

        InputType newPriority = isAutoModeActive ? InputType.Virtual : InputType.Joystick;
        inputManager.SetPriorityInputType(newPriority);
        autoCombat.SetAutoMode(isAutoModeActive);
        UpdateButtonVisual();
        
        #if DEBUG_AUTO_MODE_CHANGE
        Debug.Log($"자동 모드 업데이트 - 상태: {isAutoModeActive}, 우선순위: {newPriority}, AutoCombat: {autoCombat.IsAutoModeEnabled()}");
        #endif
    }
    
    private void ForceDisableAutoMode()
    {
        isAutoModeActive = false;
        #if DEBUG_FORCE_DISABLE
        Debug.Log("자동 모드 강제 비활성화");
        #endif
        
        if (inputManager != null)
        {
            inputManager.SetPriorityInputType(InputType.Joystick);
        }
        
        if (autoCombat != null)
        {
            autoCombat.SetAutoMode(false);
        }
        
        UpdateButtonVisual();
    }
    
    private void UpdateButtonVisual()
    {
        if (buttonImage != null)
        {
            buttonImage.color = isAutoModeActive ? activeColor : inactiveColor;
            //Debug.Log($"버튼 색상 업데이트: {(isAutoModeActive ? "활성화(초록)" : "비활성화(빨강)")}");
        }
        
        if (buttonText != null)
        {
            buttonText.text = isAutoModeActive ? "Auto: ON" : "Auto: OFF";
        }
    }
} 