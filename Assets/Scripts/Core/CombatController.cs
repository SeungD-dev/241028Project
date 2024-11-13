using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CombatController : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] private float healthPotionAmount = 20f;  // ü�� ���� ȸ����

    private PlayerStats playerStats;
    private Dictionary<ItemType, Queue<GameObject>> itemPools;
    private bool isInitialized = false;
    private void Start()
    {
        StartCoroutine(InitializeAfterGameStart());
    }

    private IEnumerator InitializeAfterGameStart()
    {
        // PlayerStats�� �ʱ�ȭ�� ������ ���
        while (GameManager.Instance.PlayerStats == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // PlayerStats �ʱ�ȭ �� ����
        playerStats = GameManager.Instance.PlayerStats;
        Debug.Log($"CombatController initialized with PlayerStats - Level: {playerStats.Level}, Exp: {playerStats.CurrentExp}");

        // �̺�Ʈ ����
        playerStats.OnHealthChanged += HandleHealthChanged;
        playerStats.OnLevelUp += HandleLevelChanged;
        playerStats.OnExpChanged += HandleExpChanged;
        playerStats.OnPlayerDeath += HandlePlayerDeath;

        // ������ Ǯ �ʱ�ȭ
        InitializeItemPools();

        isInitialized = true;
    }
    private void InitializeItemPools()
    {
        // ��� ��� ���̺� ã�Ƽ� Ǯ �ʱ�ȭ
        var dropTables = Resources.LoadAll<DropTable>("");
        foreach (var table in dropTables)
        {
            foreach (var drop in table.possibleDrops)
            {
                if (drop.itemPrefab != null)
                {
                    ObjectPool.Instance.CreatePool(
                        drop.itemType.ToString(),
                        drop.itemPrefab,
                        10  // �⺻ Ǯ ������
                    );
                }
            }
        }
    }

    public void SpawnDrops(Vector3 position, DropTable dropTable)
    {
        if (dropTable == null) return;

        float randomValue = Random.Range(0f, 100f);
        float currentRate = 0f;

        foreach (var drop in dropTable.possibleDrops)
        {
            currentRate += drop.dropRate;
            if (randomValue <= currentRate)
            {
                int amount = Random.Range(drop.minAmount, drop.maxAmount + 1);

                for (int i = 0; i < amount; i++)
                {
                    Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
                    Vector3 spawnPos = position + new Vector3(randomOffset.x, randomOffset.y, 0);

                    ObjectPool.Instance.SpawnFromPool(
                        drop.itemType.ToString(),
                        spawnPos,
                        Quaternion.identity
                    );
                }
                break;
            }
        }
    }


    // ������ ȿ�� ����
    // CombatController�� ApplyItemEffect �޼��忡 �α� �߰�
    public void ApplyItemEffect(ItemType itemType)
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats is null when trying to apply item effect!");
            return;
        }

        Debug.Log($"Before effect - Exp: {playerStats.CurrentExp}, Health: {playerStats.CurrentHealth}");

        switch (itemType)
        {
            case ItemType.ExperienceSmall:
                playerStats.AddExperience(1f);
                Debug.Log("Applied small exp +1");
                break;
            case ItemType.ExperienceLarge:
                playerStats.AddExperience(10f);
                Debug.Log("Applied large exp +10");
                break;
            case ItemType.HealthPotion:
                playerStats.Heal(healthPotionAmount);
                Debug.Log($"Applied healing {healthPotionAmount}");
                break;
        }

        Debug.Log($"After effect - Exp: {playerStats.CurrentExp}, Health: {playerStats.CurrentHealth}");
    }


    private void OnDestroy()
    {
        if(playerStats != null)
        {
            playerStats.OnHealthChanged -= HandleHealthChanged;
            playerStats.OnLevelUp -= HandleLevelChanged;
            playerStats.OnExpChanged -= HandleExpChanged;
            playerStats.OnPlayerDeath -= HandlePlayerDeath;
        }
    }

    private void HandleHealthChanged(float health)
    {
        UpdateHealthUI(health);
    }

    private void UpdateHealthUI(float health)
    {
        
    }

    private void HandleLevelChanged(int level) 
    {
        
    }

    private void HandleExpChanged(float exp)
    {
        Debug.Log($"Experience changed to: {exp}");
        // UI ������Ʈ ����
    }

    private void HandlePlayerDeath()
    {

    }
}
