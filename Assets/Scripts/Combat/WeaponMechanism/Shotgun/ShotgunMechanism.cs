using UnityEngine;
using System.Collections.Generic;

public class ShotgunMechanism : WeaponMechanism
{
    private int currentProjectileCount;
    private float currentSpreadAngle;
    private const string DESTROY_VFX_TAG = "Bullet_DestroyVFX";

    // ��ȯ Ǯ �ý��� - 2���� Ǯ�� ����Ͽ� �޸� ������� ����
    private const int POOL_COUNT = 2; // 3������ 2���� ����
    private string[] poolTags;
    private int currentPoolIndex = 0;

    // ĳ�̿� ������
    private Vector2 targetDirection = Vector2.zero;
    private Vector2 projectileDirection = Vector2.zero;
    private float baseAngle;
    private float angleStep;
    private float startAngle;
    private Vector3 spawnPosition = Vector3.zero;
    private Quaternion projectileRotation;

    // ���� ����ȭ�� ���� ���� �迭
    private GameObject[] projectileArray;

    // ����� �÷��� (��� ���忡���� false�� ����)
    private const bool ENABLE_DEBUG_LOGS = false;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);

        // ��ȯ Ǯ �±� �ʱ�ȭ
        poolTags = new string[POOL_COUNT];
        for (int i = 0; i < POOL_COUNT; i++)
        {
            poolTags[i] = $"{data.weaponType}_Projectile_{i}";
        }

        // �ִ� �ʿ� ũ���� �迭 �� ���� �Ҵ�
        int maxProjectiles = GetMinimumProjectileCountForTier(4) + 1;
        projectileArray = new GameObject[maxProjectiles];

        // VFX Ǯ �ʱ�ȭ
        if (ObjectPool.Instance != null)
        {
            GameObject vfxPrefab = Resources.Load<GameObject>("Prefabs/VFX/BulletDestroyVFX");
            if (vfxPrefab != null && !ObjectPool.Instance.DoesPoolExist(DESTROY_VFX_TAG))
            {
                ObjectPool.Instance.CreatePool(DESTROY_VFX_TAG, vfxPrefab, 30);
            }
        }

        UpdateWeaponStats();
        InitializeProjectilePools();
    }

    private void InitializeProjectilePools()
    {
        if (weaponData == null || weaponData.projectilePrefab == null) return;

        // �� Ǯ�� ũ�� ��� - �ʿ��� �ּ� �纸�� �ణ �� ũ��
        int maxTierCount = GetMinimumProjectileCountForTier(4);
        int poolSize = maxTierCount * 2; // ����� ���� Ȯ��

        for (int i = 0; i < POOL_COUNT; i++)
        {
            if (ObjectPool.Instance != null && !ObjectPool.Instance.DoesPoolExist(poolTags[i]))
            {
                ObjectPool.Instance.CreatePool(poolTags[i], weaponData.projectilePrefab, poolSize);             
            }
        }
    }

    protected override void InitializeProjectilePool()
    {
        // ������ �ʱ�ȭ �޼���� ��ü
    }

    protected override void UpdateWeaponStats()
    {
        base.UpdateWeaponStats();

        if (weaponData != null)
        {
            // Ƽ� �ּ� ����ü �� ����
            int tier = weaponData.currentTier;
            currentProjectileCount = GetMinimumProjectileCountForTier(tier);
            currentSpreadAngle = weaponData.CurrentTierStats.spreadAngle;

            // ���� ���
            if (currentProjectileCount > 1)
            {
                angleStep = currentSpreadAngle / (currentProjectileCount - 1);
            }
            else
            {
                angleStep = 0;
            }
        }
    }

    // Ƽ� �ּ� ����ü ��
    private int GetMinimumProjectileCountForTier(int tier)
    {
        switch (tier)
        {
            case 1: return 4;
            case 2: return 5;
            case 3: return 6;
            case 4: return 7;
            default: return 4;
        }
    }

    protected override void Attack(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return;

        // ���� ���
        SoundManager.Instance?.PlaySound("Shotgun_sfx", 1f, false);

        // ���� Ǯ �ε����� ��ȯ
        currentPoolIndex = (currentPoolIndex + 1) % POOL_COUNT;
        string currentTag = poolTags[currentPoolIndex];

        // ���� ���
        spawnPosition.x = playerTransform.position.x;
        spawnPosition.y = playerTransform.position.y;
        targetDirection.x = target.position.x - spawnPosition.x;
        targetDirection.y = target.position.y - spawnPosition.y;

        float magnitude = Mathf.Sqrt(targetDirection.x * targetDirection.x + targetDirection.y * targetDirection.y);
        if (magnitude > 0)
        {
            targetDirection.x /= magnitude;
            targetDirection.y /= magnitude;
        }

        baseAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        startAngle = baseAngle - (currentSpreadAngle * 0.5f);

        // ��� ����ü�� ������
        bool success = TryGetAllProjectiles(currentTag);

        if (!success)
        {
            // ���� �� �ٸ� Ǯ �õ�
            int alternateIndex = (currentPoolIndex + 1) % POOL_COUNT;
            currentTag = poolTags[alternateIndex];
            success = TryGetAllProjectiles(currentTag);

            if (!success)
            {
                // �� ��° �õ��� �����ϸ� Ǯ Ȯ�� �� ��õ�
                EnsurePoolCapacity(currentTag);
                success = TryGetAllProjectiles(currentTag);

                if (!success)
                {
                    return;
                }
            }
        }

        // ��� ����ü �ʱ�ȭ �� �߻�
        for (int i = 0; i < currentProjectileCount; i++)
        {
            GameObject projectileObj = projectileArray[i];
            if (projectileObj == null) continue;

            // ���� �� ���� ���
            float currentAngle = startAngle + (angleStep * i);
            float angleRad = currentAngle * Mathf.Deg2Rad;
            projectileDirection.x = Mathf.Cos(angleRad);
            projectileDirection.y = Mathf.Sin(angleRad);
            projectileRotation = Quaternion.Euler(0, 0, currentAngle);

            // ��ġ �� ȸ�� ����
            projectileObj.transform.position = spawnPosition;
            projectileObj.transform.rotation = projectileRotation;

            // ����ü �ʱ�ȭ
            if (projectileObj.TryGetComponent(out BaseProjectile projectile))
            {
                projectile.SetPoolTag(currentTag);
                projectile.Initialize(
                    weaponData.CalculateFinalDamage(playerStats),
                    projectileDirection,
                    weaponData.CurrentTierStats.projectileSpeed,
                    weaponData.CalculateFinalKnockback(playerStats),
                    currentRange,
                    weaponData.CalculateFinalProjectileSize(playerStats),
                    false, 0, 0f
                );
            }

            // �ʱ�ȭ �Ϸ� �� Ȱ��ȭ
            projectileObj.SetActive(true);
        }
    }

    // ��� ����ü�� �������� �õ�
    private bool TryGetAllProjectiles(string tag)
    {
        // �ʱ�ȭ
        for (int i = 0; i < currentProjectileCount; i++)
        {
            projectileArray[i] = null;
        }

        bool allSuccess = true;

        // ��� ����ü�� �� ���� ��Ȱ��ȭ ���·� ��������
        for (int i = 0; i < currentProjectileCount; i++)
        {
            GameObject proj = ObjectPool.Instance.SpawnFromPool(tag, spawnPosition, Quaternion.identity);

            if (proj != null)
            {
                proj.SetActive(false); // �ʱ�ȭ ���� ��Ȱ��ȭ
                projectileArray[i] = proj;
            }
            else
            {
                allSuccess = false;
                break;
            }
        }

        // ���� �� �̹� ������ �͵��� Ǯ�� ��ȯ
        if (!allSuccess)
        {
            for (int i = 0; i < currentProjectileCount; i++)
            {
                if (projectileArray[i] != null)
                {
                    ObjectPool.Instance.ReturnToPool(tag, projectileArray[i]);
                    projectileArray[i] = null;
                }
            }
        }

        return allSuccess;
    }

    // Ǯ �뷮 Ȯ��
    private void EnsurePoolCapacity(string tag)
    {
        // ���ɿ� ������ ���� �ʵ��� �ּ������� Ȯ��
        int existingSize = 0;

        if (ObjectPool.Instance != null)
        {
            existingSize = ObjectPool.Instance.GetAvailableCount(tag);

            // �ʿ��� ��ŭ�� �߰� (�ʿ䷮�� 2�� + ������)
            int requiredSize = currentProjectileCount * 2 + 5;
            int additionalNeeded = requiredSize - existingSize;

            if (additionalNeeded > 0 && weaponData.projectilePrefab != null)
            {
                // Ǯ�� �̹� ������ Ȯ��
                if (ObjectPool.Instance.DoesPoolExist(tag))
                {
                    for (int i = 0; i < additionalNeeded; i++)
                    {
                        GameObject newObj = Object.Instantiate(weaponData.projectilePrefab);
                        newObj.SetActive(false);
                        ObjectPool.Instance.ReturnToPool(tag, newObj);
                    }
                }
                // Ǯ�� ������ ���� ����
                else
                {
                    ObjectPool.Instance.CreatePool(tag, weaponData.projectilePrefab, requiredSize);
                }
            }
        }
    }

    public override void OnPlayerStatsChanged()
    {
        base.OnPlayerStatsChanged();
        UpdateWeaponStats();
    }
}