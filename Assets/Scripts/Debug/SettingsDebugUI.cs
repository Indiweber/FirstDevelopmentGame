// #define DEBUG_COMPONENT_NOT_FOUND
// #define DEBUG_SETTINGS_CHANGE

/* 디버그 정의
 * DEBUG_COMPONENT_NOT_FOUND: 필수 컴포넌트를 찾지 못했을 때 에러를 출력
 * DEBUG_SETTINGS_CHANGE: 설정 변경 관련 디버그 정보를 출력
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsDebugUI : MonoBehaviour
{
    [SerializeField] private MovementSettings settings;
    private bool showDebugWindow = false;
    private Rect windowRect = new Rect(10, 10, 300, 200);

    private void Start()
    {
        if (GetComponent<MovementSettings>() == null)
        {
            #if DEBUG_COMPONENT_NOT_FOUND
            Debug.LogError("MovementSettings 컴포넌트가 필요합니다!");
            #endif
            enabled = false;
        }
    }

    private void Update()
    {
        // F1 키로 디버그 창 토글
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showDebugWindow = !showDebugWindow;
        }
    }

    private void OnGUI()
    {
        if (showDebugWindow && settings != null)
        {
            windowRect = GUILayout.Window(0, windowRect, DrawDebugWindow, "Combat Settings Debug");
        }
    }

    private void DrawDebugWindow(int windowID)
    {
        GUILayout.Label("Search Radius");
        settings.searchRadius = GUILayout.HorizontalSlider(settings.searchRadius, 0f, 50f);
        GUILayout.Label($"Current: {settings.searchRadius:F1}");

        GUILayout.Space(10);

        GUILayout.Label("Dash Speed");
        settings.dashSpeed = GUILayout.HorizontalSlider(settings.dashSpeed, 0f, 30f);
        GUILayout.Label($"Current: {settings.dashSpeed:F1}");

        GUILayout.Space(10);

        GUILayout.Label("Min Enemy Distance");
        settings.minEnemyDistance = GUILayout.HorizontalSlider(settings.minEnemyDistance, 0f, 5f);
        GUILayout.Label($"Current: {settings.minEnemyDistance:F1}");

        GUI.DragWindow();
    }

    public void OnSettingChanged(float value)
    {
        #if DEBUG_SETTINGS_CHANGE
        Debug.Log($"설정 변경: {value}");
        #endif
        
        // ... existing code ...
    }
} 