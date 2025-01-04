using UnityEngine;

public enum EnemyType
{
    Hunter,
    Walker,
    Heavy
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Prefab Reference")]
    public GameObject enemyPrefab;  // �ν��Ͻ�ȭ�� �� ������

    [Header("Enemy Info")]
    public string enemyName;
    public Sprite enemySprite;
    public EnemyType enemyType;

    [Header("Base Stats")]
    public float baseHealth;
    public float maxPossibleHealth;
    public float baseDamage;
    public float moveSpeed;

    [Header("Pool Settings")]
    public int initialPoolSize = 10;  // �ʱ� Ǯ ������

    [Header("Drop Settings")]
    public EnemyDropTable dropTable;  // �⺻ ��� ���̺� (����ġ/���)

    [Header("Additional Drop Settings")]
    [Range(0f, 100f)]
    public float additionalDropRate;  // �߰� ������ ��� Ȯ��

    private void OnValidate()
    {
        // ������ ��ȿ�� ����
        if (baseHealth <= 0)
            Debug.LogError($"Invalid base health for {enemyName}: must be greater than 0");

        if (maxPossibleHealth < baseHealth)
            Debug.LogError($"Invalid max possible health for {enemyName}: must be greater than or equal to base health");

        if (baseDamage < 0)
            Debug.LogError($"Invalid base damage for {enemyName}: must be greater than or equal to 0");

        if (moveSpeed <= 0)
            Debug.LogError($"Invalid move speed for {enemyName}: must be greater than 0");

        if (initialPoolSize <= 0)
            Debug.LogError($"Invalid initial pool size for {enemyName}: must be greater than 0");

        if (dropTable == null)
            Debug.LogWarning($"No drop table assigned for {enemyName}");

        if (enemyPrefab == null)
            Debug.LogError($"No prefab assigned for {enemyName}");

        if (enemySprite == null)
            Debug.LogWarning($"No sprite assigned for {enemyName}");
    }
}