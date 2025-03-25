using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JoystickController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float joystickRadius = 50f;

    private Vector2 _rawInputVector;
    public Vector2 InputVector { get; private set; }

    private void Start()
    {
        // 컴포넌트 자동 참조
        if (background == null)
            background = transform.Find("Background")?.GetComponent<RectTransform>();
        
        if (handle == null && background != null)
            handle = background.Find("Joystick")?.GetComponent<RectTransform>();
        
        if (background == null || handle == null)
            Debug.LogError("조이스틱 설정 오류: Background 또는 Joystick 오브젝트를 찾을 수 없습니다.");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null)
            return;

        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, 
            eventData.position, 
            eventData.pressEventCamera, 
            out position);
        
        // 입력 벡터 계산 (중심점 기준)
        _rawInputVector = position / (background.sizeDelta / 2);
        
        // 벡터 크기 제한 (1 이하)
        _rawInputVector = Vector2.ClampMagnitude(_rawInputVector, 1f);
        
        // UI 좌표계와 월드 좌표계 맞추기
        // x축 값을 반전하여 왼쪽 입력이 음의 방향으로 변환되도록 수정
        InputVector = new Vector2(-_rawInputVector.x, -_rawInputVector.y);
        
        // 디버그 로그 추가
        Debug.Log($"JoystickInput: 원래={_rawInputVector}, 변환={InputVector}");
        
        // 조이스틱 핸들 위치 설정
        handle.anchoredPosition = - _rawInputVector * joystickRadius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 입력 초기화
        _rawInputVector = Vector2.zero;
        InputVector = Vector2.zero;
        
        // 조이스틱 핸들 위치 초기화
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }
} 