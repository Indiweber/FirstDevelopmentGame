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
            Debug.LogError("버튼 이미지 컴포넌트를 찾을 수 없습니다!");
            return;
        }
        
        // 플레이어 오브젝트가 지정되지 않았다면 찾기
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                Debug.LogError("플레이어 오브젝트를 찾을 수 없습니다!");
                return;
            }
        }
        
        InitializeComponents();
        
        // 버튼 클릭 이벤트 설정
        button.onClick.AddListener(ToggleAutoMode);
        
        // 초기 상태 설정 - 기본값은 비활성화로 시작
        isAutoModeActive = false;
        UpdateButtonVisual();
        
        // 시작 시 자동 이동 모드 강제 비활성화
        if (inputManager != null && autoCombat != null)
        {
            ForceDisableAutoMode();
        }
    }
    
    private void Start()
    {
        // 명시적으로 자동 모드를 비활성화 상태로 시작
        isAutoModeActive = false;
        UpdateAutoMode();
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
            Debug.Log($"플레이어 오브젝트 찾음: {playerObject.name}");
            
            // 플레이어 컴포넌트 가져오기
            inputManager = playerObject.GetComponent<InputManager>();
            autoCombat = playerObject.GetComponent<AutoCombat>();
            
            if (inputManager == null)
            {
                Debug.LogError($"InputManager 컴포넌트를 찾을 수 없습니다! 플레이어: {playerObject.name}");
            }
            else
            {
                Debug.Log("InputManager 컴포넌트를 찾았습니다.");
            }
            
            if (autoCombat == null)
            {
                Debug.LogError($"AutoCombat 컴포넌트를 찾을 수 없습니다! 플레이어: {playerObject.name}");
            }
            else
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
            Debug.Log($"플레이어 오브젝트 설정: {player.name}");
            InitializeComponents();
            
            // 설정 후 초기 상태 업데이트
            isAutoModeActive = false;
            UpdateAutoMode();
        }
        else
        {
            Debug.LogError("SetPlayer: 전달된 플레이어 오브젝트가 null입니다!");
        }
    }
    
    public void ToggleAutoMode()
    {
        if (inputManager == null || autoCombat == null)
        {
            Debug.LogError("필수 컴포넌트가 없어 자동 모드를 전환할 수 없습니다!");
            return;
        }

        isAutoModeActive = !isAutoModeActive;
        Debug.Log($"자동 모드 토글: {isAutoModeActive}");
        
        UpdateAutoMode();
    }
    
    // 자동 모드 상태 직접 설정 (UIManager에서 사용)
    public void SetAutoMode(bool active)
    {
        if (inputManager == null || autoCombat == null)
        {
            Debug.LogError("필수 컴포넌트가 없어 자동 모드를 설정할 수 없습니다!");
            
            // 컴포넌트 다시 찾기 시도
            if (playerObject != null)
            {
                inputManager = playerObject.GetComponent<InputManager>();
                autoCombat = playerObject.GetComponent<AutoCombat>();
                
                if (inputManager == null || autoCombat == null)
                {
                    Debug.LogError("컴포넌트를 찾을 수 없습니다. 자동 모드를 설정할 수 없습니다!");
                    return;
                }
            }
            else
            {
                return;
            }
        }
        
        // 상태 설정
        isAutoModeActive = active;
        
        // AutoCombat에 먼저 설정 (AutoCombat에서 inputManager 설정도 함께 수행)
        autoCombat.SetAutoMode(active);
        
        // 추가적으로 InputManager 우선순위도 별도로 설정
        InputType newPriority = active ? InputType.Virtual : InputType.Joystick;
        inputManager.SetPriorityInputType(newPriority);
        
        // 버튼 상태 업데이트
        UpdateButtonVisual();
        
        Debug.Log($"자동 모드 변경 완료: {active}, 우선순위: {newPriority}, AutoCombat 상태: {autoCombat.IsAutoModeEnabled()}");
        
        // 설정 후 실제 적용 상태 확인 로그
        if (active != autoCombat.IsAutoModeEnabled())
        {
            Debug.LogError($"자동 모드 설정 불일치: 요청={active}, 실제={autoCombat.IsAutoModeEnabled()}");
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

        // 입력 우선순위 설정
        InputType newPriority = isAutoModeActive ? InputType.Virtual : InputType.Joystick;
        inputManager.SetPriorityInputType(newPriority);
        
        // AutoCombat 설정
        autoCombat.SetAutoMode(isAutoModeActive);
        
        // 버튼 상태 업데이트
        UpdateButtonVisual();
        
        Debug.Log($"자동 모드 업데이트 - 상태: {isAutoModeActive}, 우선순위: {newPriority}, AutoCombat: {autoCombat.IsAutoModeEnabled()}");
    }
    
    private void ForceDisableAutoMode()
    {
        isAutoModeActive = false;
        Debug.Log("자동 모드 강제 비활성화");
        
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
            Debug.Log($"버튼 색상 업데이트: {(isAutoModeActive ? "활성화(초록)" : "비활성화(빨강)")}");
        }
        
        if (buttonText != null)
        {
            buttonText.text = isAutoModeActive ? "Auto: ON" : "Auto: OFF";
        }
    }
} 