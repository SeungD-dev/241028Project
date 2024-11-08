using UnityEngine;

[System.Serializable]
public class EnemySpawnWeight
{
    public EnemyData enemyData;
    public AnimationCurve spawnWeightOverTime;
    [Tooltip("�ּ� ���� �ð�(��)")]
    public float minGameTime = 0f;
    [Tooltip("�ִ� ���� �ð�(��)")]
    public float maxGameTime = float.MaxValue;
}

