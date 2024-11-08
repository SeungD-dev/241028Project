using UnityEngine;

[CreateAssetMenu(fileName = "EnemySpawnDatabase", menuName = "Scriptable Objects/EnemySpawnDatabase")]
public class EnemySpawnDatabase : ScriptableObject
{
    [Header("Enemy Spawn Settings")]
    public EnemySpawnWeight[] enemySpawnWeights;

    public EnemyData GetRandomEnemy(float gameTime)
    {
        float totalWeight = 0f;

        // ���� �ð��� ���� ������ ������ ����ġ �հ� ���
        foreach (var spawnWeight in enemySpawnWeights)
        {
            if (gameTime >= spawnWeight.minGameTime && gameTime <= spawnWeight.maxGameTime)
            {
                totalWeight += spawnWeight.spawnWeightOverTime.Evaluate(gameTime);
            }
        }

        if (totalWeight <= 0f)
            return null;

        // ���� ����ġ ����
        float randomWeight = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        // ���õ� ����ġ�� �ش��ϴ� �� ��ȯ
        foreach (var spawnWeight in enemySpawnWeights)
        {
            if (gameTime >= spawnWeight.minGameTime && gameTime <= spawnWeight.maxGameTime)
            {
                currentWeight += spawnWeight.spawnWeightOverTime.Evaluate(gameTime);
                if (randomWeight <= currentWeight)
                {
                    return spawnWeight.enemyData;
                }
            }
        }

        return null;
    }
}
