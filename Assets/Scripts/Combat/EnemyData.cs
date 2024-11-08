using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Prefab Reference")]
    public GameObject enemyPrefab;  // �ν��Ͻ�ȭ�� �� ������

    [Header("Enemy Info")]
    public string enemyName;
    public Sprite enemySprite;

    [Header("Base Stats")]
    public float baseHealth;
    public float maxPossibleHealth;
    public float baseDamage;
    public float moveSpeed;

    [Header("Pool Settings")]
    public int initialPoolSize = 10;  // �ʱ� Ǯ ������
}

