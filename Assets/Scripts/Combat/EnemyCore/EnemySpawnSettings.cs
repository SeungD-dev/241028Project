using UnityEngine;
//������
[System.Serializable]
public class EnemySpawnSettings
{
    public EnemyData enemyData;

    [Header("Spawn Probability Settings")]
    [Tooltip("�ð��� ���� ���� Ȯ�� Ŀ�� (X: �ð�(��), Y: 0~1 Ȯ��)")]
    public AnimationCurve spawnProbabilityCurve = new AnimationCurve(
        new Keyframe(0, 0),
        new Keyframe(15, 1)
    );

    [Tooltip("�ִ� ���� Ȯ�� (%)")]
    [Range(0f, 100f)]
    public float maxSpawnWeight = 100f;

    [Header("Ratio Control")]
    [Tooltip("��ü ���� �� �� ���� �ּ� ���� (%)")]
    [Range(0f, 100f)]
    public float minSpawnRatio = 0f;

    [Tooltip("��ü ���� �� �� ���� �ִ� ���� (%)")]
    [Range(0f, 100f)]
    public float maxSpawnRatio = 100f;

    // ���� ���� �� ����
    [System.NonSerialized]
    public int spawnCount = 0;

    public float GetSpawnWeight(float gameTimeMinutes)
    {
        return spawnProbabilityCurve.Evaluate(gameTimeMinutes) * maxSpawnWeight;
    }
}