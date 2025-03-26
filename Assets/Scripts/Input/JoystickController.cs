using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class JoystickController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float joystickRadius = 50f;
    [SerializeField] private float handleRange = 1f;
    [SerializeField] private float deadZone = 0f;
    [SerializeField] private bool snapX;
    [SerializeField] private bool snapY;
    [SerializeField] private bool hideOnRelease = true;
    [SerializeField] private bool enableDebugLogs = false;

    private Vector2 _rawInputVector;
    private Vector2 _inputVector;
    public Vector2 InputVector => _inputVector;
    private Canvas canvas;
    private Camera cam;
    private bool canDebugLog = true;
    private Coroutine debugCooldownCoroutine;

    private void Start()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // 컴포넌트 자동 참조
        if (background == null)
            background = transform.Find("Background")?.GetComponent<RectTransform>();
        
        if (handle == null && background != null)
            handle = background.Find("Joystick")?.GetComponent<RectTransform>();
        
        if (background == null || handle == null)
            Debug.LogError("조이스틱 설정 오류: Background 또는 Joystick 오브젝트를 찾을 수 없습니다.");

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            Debug.LogError("The Joystick is not placed inside a canvas");
            
        SetupJoystickTransform();
    }

    private void SetupJoystickTransform()
    {
        Vector2 center = new Vector2(0.5f, 0.5f);
        background.pivot = center;
        handle.anchorMin = center;
        handle.anchorMax = center;
        handle.pivot = center;
        handle.anchoredPosition = Vector2.zero;
        
        if (hideOnRelease)
        {
            background.gameObject.SetActive(false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (hideOnRelease)
        {
            background.gameObject.SetActive(true);
        }
        
        background.position = eventData.position;
        handle.anchoredPosition = Vector2.zero;
        
        // 입력 시작 시 디버그 로그
        LogJoystickDebug(eventData.position, Vector2.zero, Vector2.zero);
        
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log($"이것은 최초 디버그 : {eventData.position}");

        if (background == null || handle == null)
            return;

        cam = null;
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            cam = canvas.worldCamera;
            
        Vector2 joystickCenter = RectTransformUtility.WorldToScreenPoint(cam, background.position);
        // radius는 항상 양수값을 사용
        Vector2 radius = new Vector2(Mathf.Abs(background.sizeDelta.x), Mathf.Abs(background.sizeDelta.y)) / 2;
        
        // 입력 방향 계산 (입력 위치 - 조이스틱 중심점)
        _rawInputVector = (eventData.position - joystickCenter) / (radius * canvas.scaleFactor);
         Debug.Log($"이것은 로우인풋벡터터 디버그 : {_rawInputVector}");
        
        if (_rawInputVector.magnitude > deadZone)
        {
            if (_rawInputVector.magnitude > 1)
                _rawInputVector = _rawInputVector.normalized;
            
            _inputVector = _rawInputVector;
            
            // UI 표시를 위한 핸들 위치 계산
            Vector2 handlePosition = _rawInputVector * joystickRadius;
            handle.anchoredPosition = handlePosition;
            
            // 디버그 로그
            LogJoystickDebug(eventData.position, _rawInputVector, handlePosition);
        }
        else
        {
            _rawInputVector = Vector2.zero;
            _inputVector = Vector2.zero;
            handle.anchoredPosition = Vector2.zero;
        }
    }

    private void LogJoystickDebug(Vector2 inputPosition, Vector2 calculatedDirection, Vector2 handlePosition)
    {
        if (!enableDebugLogs || !canDebugLog) return;
        
        if (debugCooldownCoroutine != null)
        {
            StopCoroutine(debugCooldownCoroutine);
        }
        debugCooldownCoroutine = StartCoroutine(DebugCooldown());

        // 현재 조이스틱의 중심점 (background의 위치)
        Vector2 joystickCenter = RectTransformUtility.WorldToScreenPoint(cam, background.position);
        
        // 예상 목표 지점 계산 (조이스틱 중심점 + 방향 * 반지름)
        Vector2 expectedTargetPosition = joystickCenter + (calculatedDirection * (background.sizeDelta.x / 2));
        
        Debug.Log($"[조이스틱 디버그]\n" +
                 $"1. 입력 위치: {inputPosition}\n" +
                 $"2. 조이스틱 중심: {joystickCenter}\n" +
                 $"3. 계산된 방향: {calculatedDirection} (크기: {calculatedDirection.magnitude:F2})\n" +
                 $"4. 예상 목표 지점: {expectedTargetPosition}\n" +
                 $"5. 실제 핸들 위치: {handlePosition}");
    }

    private IEnumerator DebugCooldown()
    {
        canDebugLog = false;
        yield return new WaitForSeconds(1f);
        canDebugLog = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _rawInputVector = Vector2.zero;
        _inputVector = Vector2.zero;
        
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
        
        if (hideOnRelease)
        {
            background.gameObject.SetActive(false);
        }
        
        // 디버그 코루틴 정리
        if (debugCooldownCoroutine != null)
        {
            StopCoroutine(debugCooldownCoroutine);
            debugCooldownCoroutine = null;
        }
    }

    public Vector2 GetJoystickValue()
    {
        return new Vector2(
            SnapFloat(_inputVector.x, snapX),
            SnapFloat(_inputVector.y, snapY)
        );
    }
    
    private float SnapFloat(float value, bool snap)
    {
        if (snap)
        {
            if (value > 0)
                return 1;
            if (value < 0)
                return -1;
            return 0;
        }
        return value;
    }

    private void OnDestroy()
    {
        if (debugCooldownCoroutine != null)
        {
            StopCoroutine(debugCooldownCoroutine);
        }
    }
} 