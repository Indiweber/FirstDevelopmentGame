using UnityEngine;

public class EnemyData
{
    public GameObject Enemy { get; private set; }
    public float Distance { get; private set; }
    public float Weight { get; set; }
    
    public EnemyData(GameObject enemy, float distance, float initialWeight = 1f)
    {
        Enemy = enemy;
        Distance = distance;
        Weight = initialWeight;
    }
    
    public void UpdateDistance(Vector3 fromPosition)
    {
        if (Enemy != null)
        {
            Distance = Vector3.Distance(fromPosition, Enemy.transform.position);
        }
    }
} 