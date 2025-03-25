using UnityEngine;

public class SettingsDebugUI : MonoBehaviour
{
    [SerializeField] private MovementSettings settings;
    private bool showDebugWindow = false;
    private Rect windowRect = new Rect(10, 10, 300, 200);

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
} 