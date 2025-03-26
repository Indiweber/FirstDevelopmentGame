using UnityEngine;
using UnityEngine.Events;

public class HitboxTrigger : MonoBehaviour
{
    // 이벤트 선언
    public UnityEvent<GameObject> OnEnemyEntered = new UnityEvent<GameObject>();
    public UnityEvent<GameObject> OnEnemyExited = new UnityEvent<GameObject>();
    
    private HitboxController controller;
    private string targetTag;
    private bool debugEnabled;
    
    public void Initialize(HitboxController controller, string targetTag, bool debugEnabled)
    {
        this.controller = controller;
        this.targetTag = targetTag;
        this.debugEnabled = debugEnabled;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            OnEnemyEntered?.Invoke(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            OnEnemyExited?.Invoke(other.gameObject);
        }
    }
} 