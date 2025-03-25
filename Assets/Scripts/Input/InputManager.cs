using UnityEngine;

// 조이스틱 입력 인터페이스 정의
public interface IJoystickInput
{
    Vector2 GetJoystickInput();
}

public enum InputType
{
    Joystick,
    Virtual
}

public class InputManager : MonoBehaviour, IVirtualInputReceiver
{
    [Header("입력 설정")]
    [SerializeField] private bool useVirtualInput = false;  // 기본값을 false로 변경
    [SerializeField] private bool useJoystick = true;      // 조이스틱 사용 여부
    [SerializeField] private InputType priorityInput = InputType.Joystick;  // 우선순위 입력 방식
    [SerializeField] private bool enableDebugLogs = false;  // 디버그 로그 활성화 여부

    [Header("조이스틱 참조")]
    [SerializeField] private JoystickController joystickController;

    private Vector2 virtualInput = Vector2.zero;
    private bool _virtualInputEnabled = false;

    private void Awake()
    {
        // 조이스틱 컨트롤러가 없으면 찾기
        if (joystickController == null)
        {
            joystickController = FindObjectOfType<JoystickController>();
            if (joystickController == null)
            {
                Debug.LogError("JoystickController를 찾을 수 없습니다!");
            }
        }

        // 기본 설정 - 가상 입력 비활성화, 조이스틱 우선순위
        useVirtualInput = false;
        _virtualInputEnabled = false;
        useJoystick = true;
        priorityInput = InputType.Joystick;
    }

    private void Start()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"InputManager 시작 - 가상 입력: {useVirtualInput}, 조이스틱: {useJoystick}, 우선순위: {priorityInput}");
        }
    }

    private void Update()
    {
        // 가상 입력이 활성화된 경우 가상 입력 사용
        if (_virtualInputEnabled && priorityInput == InputType.Virtual)
        {
            // 입력이 있을 때만 로그 출력
            if (virtualInput.magnitude > 0.1f)
            {
                Debug.Log($"[가상 입력 사용 중] 값: {virtualInput}, 크기: {virtualInput.magnitude:F3}");
            }
        }
        else
        {
            // 가상 입력 비활성화 시 virtualInput 초기화
            if (virtualInput != Vector2.zero)
            {
                virtualInput = Vector2.zero;
            }
        }
    }

    // 가상 입력 설정 (IVirtualInputReceiver 인터페이스 구현)
    public void SetVirtualInput(Vector2 input)
    {
        virtualInput = input;
        _virtualInputEnabled = true;  // 가상 입력이 설정되면 자동으로 활성화
        
        // 강제로 우선순위 설정
        if (priorityInput != InputType.Virtual && input.magnitude > 0)
        {
            priorityInput = InputType.Virtual;
            Debug.Log($"[가상 입력] 우선순위 자동 변경: {priorityInput}");
        }
        
        // 항상 로그 출력하도록 변경
        Debug.Log($"[가상 입력 설정] 입력값: {input}, 크기: {input.magnitude:F3}");
    }

    // 입력 가져오기 (Player 스크립트에서 호출)
    public Vector2 GetMovementInput()
    {
        // 우선순위에 따른 입력 선택
        if (priorityInput == InputType.Virtual && _virtualInputEnabled)
        {
            // 로그 추가
            if (virtualInput.magnitude > 0.1f)
            {
                Debug.Log($"[입력 반환] 가상 입력: {virtualInput}");
            }
            return virtualInput;
        }
        else if (useJoystick && joystickController != null)
        {
            Vector2 joystickValue = joystickController.InputVector;
            // 로그 추가
            if (joystickValue.magnitude > 0.1f)
            {
                Debug.Log($"[입력 반환] 조이스틱 입력: {joystickValue}");
            }
            return joystickValue;
        }

        // 입력 없음
        return Vector2.zero;
    }
    
    // 조이스틱 입력 활성화/비활성화
    public void SetJoystickEnabled(bool enabled)
    {
        useJoystick = enabled;
        
        if (!enabled)
        {
            // 조이스틱 비활성화 시 가상 입력 우선
            _virtualInputEnabled = true;
            priorityInput = InputType.Virtual;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[조이스틱 설정] 활성화: {enabled}, 가상 입력 우선: {_virtualInputEnabled}");
        }
    }

    // 입력 우선순위 설정
    public void SetPriorityInputType(InputType inputType)
    {
        if (priorityInput != inputType)
        {
            priorityInput = inputType;
            
            if (enableDebugLogs)
            {
                Debug.Log($"입력 우선순위 변경: {inputType}");
            }
        }
    }

    // 입력 우선순위 가져오기
    public InputType GetPriorityInput()
    {
        return priorityInput;
    }
} 