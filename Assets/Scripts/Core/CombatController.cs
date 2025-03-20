using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class CombatController : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] private float healthPotionAmount = 20f;

    [Header("Magnet Effect Setting")]
    [SerializeField] private float magnetForce = 20f;

    [Header("Death Effect Settings")]
    [SerializeField] private string deathEffectPoolTag = "DeathParticle";
    [SerializeField] private int particlesPerEffect = 5;
    [SerializeField] private float explosionRadius = 1f;
    [SerializeField] private float explosionDuration = 0.5f;
    [SerializeField] private Vector2 particleSizeRange = new Vector2(0.1f, 0.3f);
    [SerializeField]
    private Color[] deathParticleColors = new Color[]
    {
        new Color(1f, 0f, 0f),      // ����
        new Color(0f, 0f, 0f),      // ����
        new Color(65/255f, 65/255f, 65/255f)  // ȸ��
    };

    // ��ƼŬ ����ȭ�� ���� ����
    private int maxConcurrentDeathEffects = 5;
    private int activeDeathEffectsCount = 0;

    // ĳ��
    private static readonly WaitForSeconds particleDelay = new WaitForSeconds(0.02f);

    private List<CollectibleItem> activeCollectibles = new List<CollectibleItem>();
    private PlayerStats playerStats;
    private bool isInitialized = false;

    private void Start()
    {
        StartCoroutine(WaitForInitialization());
    }

    private IEnumerator WaitForInitialization()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (!GameManager.Instance.IsInitialized && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (elapsed >= timeout)
        {
            Debug.LogError("Scene initialization timed out!");
            yield break;
        }

        InitializeCombatSystem();
    }

    private void InitializeCombatSystem()
    {
        playerStats = GameManager.Instance.PlayerStats;

        if (playerStats != null)
        {
            playerStats.OnPlayerDeath += HandlePlayerDeath;
            InitializeItemPools();
            isInitialized = true;
        }
    }

    public void PlayEnemyDeathEffect(Vector3 position, Color? customColor = null,float enemyScale = 1f)
    {
        if (!isInitialized || ObjectPool.Instance == null)
            return;

        // �ִ� ���� ����Ʈ �� ����
        if (activeDeathEffectsCount >= maxConcurrentDeathEffects)
            return;

        // ����Ʈ Ǯ Ȯ��
        if (!ObjectPool.Instance.DoesPoolExist(deathEffectPoolTag))
        {
            Debug.LogWarning($"Death effect pool '{deathEffectPoolTag}' not found!");
            return;
        }

        StartCoroutine(CreateDeathEffect(position, customColor,enemyScale));
    }

    private IEnumerator CreateDeathEffect(Vector3 position, Color? customColor,float enemyScale)
    {
        activeDeathEffectsCount++;

        for (int i = 0; i < particlesPerEffect; i++)
        {
            // ������Ʈ Ǯ���� ��ƼŬ ��������
            GameObject particle = ObjectPool.Instance.SpawnFromPool(
                deathEffectPoolTag,
                position,
                Quaternion.identity
            );

            if (particle != null)
            {
                // ��ƼŬ ���� �� �ִϸ��̼�
                AnimateDeathParticle(particle, position, customColor,enemyScale);
            }

            // �ణ�� �ð����� �ΰ� ��ƼŬ ����
            yield return particleDelay;
        }

        // ����Ʈ�� ���� ������ ���
        yield return new WaitForSeconds(explosionDuration);

        activeDeathEffectsCount--;
    }

    private void AnimateDeathParticle(GameObject particle, Vector3 position, Color? customColor,float enemyScale)
    {
        SpriteRenderer renderer = particle.GetComponent<SpriteRenderer>();
        if (renderer == null) return;

        // �� ũ�⿡ �°� ��ƼŬ ũ�� ����
        float baseSize = Random.Range(particleSizeRange.x, particleSizeRange.y);
        float scaledSize = baseSize * enemyScale; // �� ũ�⿡ ����Ͽ� ����

        // ���� ������ �� ũ�⿡ ����Ͽ� ����
        float adjustedRadius = explosionRadius * enemyScale;

        // ������ ����
        Color color = customColor ?? deathParticleColors[Random.Range(0, deathParticleColors.Length)];
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = adjustedRadius * Random.Range(0.5f, 1f);

        // ��ǥ ��ġ ���
        Vector3 targetPos = position + new Vector3(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance,
            0f
        );

        // ���� Ʈ�� ����
        DOTween.Kill(particle.transform);
        DOTween.Kill(renderer);

        // �ʱ� ����
        particle.transform.localScale = Vector3.zero;
        renderer.color = new Color(color.r, color.g, color.b, 1f);

        // �ִϸ��̼� ������ ����
        Sequence seq = DOTween.Sequence();

        // ũ�� �ִϸ��̼�
        seq.Append(particle.transform.DOScale(new Vector3(scaledSize, scaledSize, 1f), explosionDuration * 0.2f));

        // �̵� �ִϸ��̼�
        seq.Join(particle.transform.DOMove(targetPos, explosionDuration)
            .SetEase(Ease.OutQuad));

        // ȸ�� �ִϸ��̼�
        seq.Join(particle.transform.DORotate(
            new Vector3(0, 0, Random.Range(-180f, 180f)),
            explosionDuration,
            RotateMode.FastBeyond360
        ).SetEase(Ease.OutQuad));

        // ���̵� �ƿ�
        seq.Join(renderer.DOFade(0f, explosionDuration)
            .SetEase(Ease.InQuad));

        // �Ϸ� �� Ǯ�� ��ȯ
        seq.OnComplete(() => {
            ObjectPool.Instance.ReturnToPool(deathEffectPoolTag, particle);
        });

        // Ÿ�ӽ����Ͽ� ������� �ʵ��� ����
        seq.SetUpdate(true);
    }

    public void SpawnDrops(Vector3 position, EnemyDropTable dropTable)
    {
        if (!isInitialized)
        {
            Debug.LogError("CombatController is not initialized!");
            return;
        }

        if (dropTable == null)
        {
            Debug.LogError("DropTable is null!");
            return;
        }

        bool essentialDropSpawned = false;
        int maxAttempts = 3;  // �ִ� �õ� Ƚ��
        int attempts = 0;

        // Essential Drop (Experience or Gold) - ������ ������ �õ�
        while (!essentialDropSpawned && attempts < maxAttempts)
        {
            float randomValue = Random.Range(0f, 100f);
            if (randomValue <= dropTable.experienceDropRate)
            {
                GameObject expDrop = SpawnExperienceDrop(position, dropTable.experienceInfo);
                essentialDropSpawned = (expDrop != null);
            }
            else
            {
                GameObject goldDrop = SpawnGoldDrop(position, dropTable.goldInfo);
                essentialDropSpawned = (goldDrop != null);
            }
            attempts++;
        }

        if (!essentialDropSpawned)
        {
            Debug.LogError("Failed to spawn essential drop after multiple attempts!");
        }

        // Additional Drop - Enemy�� Die()���� ó���ϵ��� ����
    }


    private GameObject SpawnExperienceDrop(Vector3 position, ExperienceDropInfo expInfo)
    {
        if (expInfo == null)
        {
            Debug.LogError("ExperienceDropInfo is null!");
            return null;
        }

        float randomValue = Random.Range(0f, 100f);
        ItemType selectedType;

        if (randomValue <= expInfo.smallExpRate)
        {
            selectedType = ItemType.ExperienceSmall;
        }
        else if (randomValue <= expInfo.smallExpRate + expInfo.mediumExpRate)
        {
            selectedType = ItemType.ExperienceMedium;
        }
        else
        {
            selectedType = ItemType.ExperienceLarge;
        }

        Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
        Vector3 spawnPos = position + new Vector3(randomOffset.x, randomOffset.y, 0);

        GameObject spawnedObj = ObjectPool.Instance.SpawnFromPool(
            selectedType.ToString(),
            spawnPos,
            Quaternion.identity
        );

        if (spawnedObj == null)
        {
            Debug.LogError($"Failed to spawn experience item of type: {selectedType}");
        }

        return spawnedObj;
    }


    private GameObject SpawnGoldDrop(Vector3 position, GoldDropInfo goldInfo)
    {
        if (goldInfo == null)
        {
            Debug.LogError("GoldDropInfo is null!");
            return null;
        }

        Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
        Vector3 spawnPos = position + new Vector3(randomOffset.x, randomOffset.y, 0);

        GameObject goldObj = ObjectPool.Instance.SpawnFromPool(
            ItemType.Gold.ToString(),
            spawnPos,
            Quaternion.identity
        );

        if (goldObj != null)
        {
            int goldAmount = Random.Range(goldInfo.minGoldAmount, goldInfo.maxGoldAmount + 1);
            if (goldObj.TryGetComponent<CollectibleItem>(out var collectible))
            {
                collectible.SetGoldAmount(goldAmount);
            }
        }
        else
        {
            Debug.LogError("Failed to spawn gold item");
        }

        return goldObj;
    }

    public void SpawnAdditionalDrop(Vector3 position, AdditionalDrop dropInfo)
    {
        GameObject item = SpawnItem(position, dropInfo.itemType);
        if (item.TryGetComponent<CollectibleItem>(out var collectible))
        {
            collectible.Initialize(dropInfo);
        }
    }

    private GameObject SpawnItem(Vector3 position, ItemType itemType)
    {
        Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
        Vector3 spawnPos = position + new Vector3(randomOffset.x, randomOffset.y, 0);

        return ObjectPool.Instance.SpawnFromPool(
            itemType.ToString(),
            spawnPos,
            Quaternion.identity
        );
    }

    public void ApplyItemEffect(ItemType itemType, int goldAmount = 0)
    {
        if (GameManager.Instance?.PlayerStats == null)
        {
            Debug.LogError("PlayerStats is null when trying to apply item effect!");
            return;
        }

        var playerStats = GameManager.Instance.PlayerStats;

        switch (itemType)
        {
            case ItemType.ExperienceSmall:
                playerStats.AddExperience(1f);
                break;
            case ItemType.ExperienceMedium:
                playerStats.AddExperience(7f);
                break;
            case ItemType.ExperienceLarge:
                playerStats.AddExperience(25f);
                break;
            case ItemType.Gold:
                playerStats.AddCoins(goldAmount);
                break;
            case ItemType.HealthPotion:
                playerStats.Heal(healthPotionAmount);
                break;
            case ItemType.Magnet:
                ApplyMagnetEffect();
                break;
            default:
                Debug.LogWarning($"Unknown item type: {itemType}");
                break;
        }
    }

    private void ApplyMagnetEffect()
    {
        foreach (var item in activeCollectibles.ToList())
        {
            if (item != null)
            {
                item.PullToPlayer(magnetForce);
            }
        }
    }

    public void RegisterCollectible(CollectibleItem item)
    {
        if (!activeCollectibles.Contains(item))
        {
            activeCollectibles.Add(item);
        }
    }

    public void UnregisterCollectible(CollectibleItem item)
    {
        activeCollectibles.Remove(item);
    }

    private void HandlePlayerDeath()
    {
        isInitialized = false;
        GameManager.Instance.SetGameState(GameState.GameOver);
    }

    private void InitializeItemPools()
    {
        var dropTables = Resources.LoadAll<EnemyDropTable>("");
        var processedPrefabs = new HashSet<GameObject>();

        foreach (var table in dropTables)
        {
            // �⺻ ����ġ Ǯ �ʱ�ȭ
            if (table.experienceInfo != null)
            {
                if (!processedPrefabs.Contains(table.experienceInfo.smallExpPrefab) &&
                    !ObjectPool.Instance.DoesPoolExist(ItemType.ExperienceSmall.ToString()))
                {
                    ObjectPool.Instance.CreatePool(ItemType.ExperienceSmall.ToString(),
                        table.experienceInfo.smallExpPrefab, 10);
                    processedPrefabs.Add(table.experienceInfo.smallExpPrefab);
                }

                if (!processedPrefabs.Contains(table.experienceInfo.mediumExpPrefab) &&
                    !ObjectPool.Instance.DoesPoolExist(ItemType.ExperienceMedium.ToString()))
                {
                    ObjectPool.Instance.CreatePool(ItemType.ExperienceMedium.ToString(),
                        table.experienceInfo.mediumExpPrefab, 10);
                    processedPrefabs.Add(table.experienceInfo.mediumExpPrefab);
                }

                if (!processedPrefabs.Contains(table.experienceInfo.largeExpPrefab) &&
                    !ObjectPool.Instance.DoesPoolExist(ItemType.ExperienceLarge.ToString()))
                {
                    ObjectPool.Instance.CreatePool(ItemType.ExperienceLarge.ToString(),
                        table.experienceInfo.largeExpPrefab, 10);
                    processedPrefabs.Add(table.experienceInfo.largeExpPrefab);
                }
            }

            // ��� Ǯ �ʱ�ȭ
            if (table.goldInfo != null && !processedPrefabs.Contains(table.goldInfo.goldPrefab) &&
                !ObjectPool.Instance.DoesPoolExist(ItemType.Gold.ToString()))
            {
                ObjectPool.Instance.CreatePool(ItemType.Gold.ToString(),
                    table.goldInfo.goldPrefab, 10);
                processedPrefabs.Add(table.goldInfo.goldPrefab);
            }

            // �߰� ������ Ǯ �ʱ�ȭ
            if (table.additionalDrops != null)
            {
                foreach (var drop in table.additionalDrops)
                {
                    if (drop.itemPrefab != null && !processedPrefabs.Contains(drop.itemPrefab) &&
                        !ObjectPool.Instance.DoesPoolExist(drop.itemType.ToString()))
                    {
                        ObjectPool.Instance.CreatePool(drop.itemType.ToString(), drop.itemPrefab, 10);
                        processedPrefabs.Add(drop.itemPrefab);
                    }
                }
            }
        }

        // ��� ����Ʈ Ǯ �ʱ�ȭ (���� Ǯ�� ���� ���)
        if (!ObjectPool.Instance.DoesPoolExist(deathEffectPoolTag))
        {
            GameObject particlePrefab = Resources.Load<GameObject>($"Prefabs/VFX/{deathEffectPoolTag}");
            if (particlePrefab != null)
            {
                // ������ Ǯ ũ�� ���: ���� ����Ʈ �� * ��ƼŬ ��
                int poolSize = maxConcurrentDeathEffects * particlesPerEffect;
                ObjectPool.Instance.CreatePool(deathEffectPoolTag, particlePrefab, poolSize);
                Debug.Log($"Death effect pool initialized with {poolSize} particles");
            }
            else
            {
                Debug.LogWarning($"Death particle prefab not found: {deathEffectPoolTag}");
            }
        }
    }
    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath -= HandlePlayerDeath;
        }

        DOTween.Kill(transform);
    }
}