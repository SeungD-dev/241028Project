using System.Linq;
using UnityEngine;

public abstract class WeaponMechanism
{
    protected WeaponData weaponData;
    protected Transform playerTransform;
    protected PlayerStats playerStats;
    protected string poolTag;

    protected float lastAttackTime;
    protected float currentAttackDelay;
    protected float currentRange;
    protected float detectionRange; // �� ���� ����

    public virtual void Initialize(WeaponData data, Transform player)
    {
        weaponData = data;
        playerTransform = player;
        playerStats = player.GetComponent<PlayerStats>();
        lastAttackTime = 0f;

        UpdateWeaponStats();
        InitializeProjectilePool();
    }

    protected virtual void UpdateWeaponStats()
    {
        if (weaponData == null || playerStats == null) return;

        // PlayerStats�� ����� ���� ���� ������ ���
        currentAttackDelay = weaponData.CalculateFinalAttackDelay(playerStats);

        // ���� ��Ÿ� ��� (AOE�� ������ ���� ����)
        currentRange = weaponData.CalculateFinalRange(playerStats);

        // ���� ������ ���� ��Ÿ� + 1
        detectionRange = currentRange + 1f;
    }

    protected virtual void InitializeProjectilePool()
    {
        poolTag = $"{weaponData.weaponType}Projectile";
        if (weaponData.projectilePrefab != null)
        {
            ObjectPool.Instance.CreatePool(poolTag, weaponData.projectilePrefab, 10);
        }
        else
        {
            Debug.LogError($"Projectile prefab is missing for weapon: {weaponData.weaponName}");
        }
    }

    public virtual void UpdateMechanism()
    {
        if (Time.time >= lastAttackTime + currentAttackDelay)
        {
            Transform target = FindNearestTarget();
            if (target != null)
            {
                Attack(target);
                lastAttackTime = Time.time;
            }
        }
    }

    protected virtual Transform FindNearestTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        return enemies
            .Select(e => e.transform)
            .Where(t => Vector2.Distance(playerTransform.position, t.position) <= detectionRange)
            .OrderBy(t => Vector2.Distance(playerTransform.position, t.position))
            .FirstOrDefault();
    }

    protected abstract void Attack(Transform target);

    protected virtual void FireProjectile(Transform target)
    {
        if (target == null) return;

        Vector2 direction = (target.position - playerTransform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            playerTransform.position,
            Quaternion.Euler(0, 0, angle)
        );

        BaseProjectile projectile = projectileObj.GetComponent<BaseProjectile>();
        if (projectile != null)
        {
            float damage = weaponData.CalculateFinalDamage(playerStats);
            float knockbackPower = weaponData.CalculateFinalKnockback(playerStats);
            float projectileSpeed = weaponData.CurrentTierStats.projectileSpeed;
            float projectileSize = weaponData.CalculateFinalProjectileSize(playerStats);
            var penetrationInfo = weaponData.GetPenetrationInfo();

            projectile.Initialize(
                damage,
                direction,
                projectileSpeed,
                knockbackPower,
                currentRange,
                projectileSize,
                penetrationInfo.canPenetrate,
                penetrationInfo.maxCount,
                penetrationInfo.damageDecay
            );
        }
    }

    public WeaponData GetWeaponData()
    {
        return weaponData;
    }

    public virtual void OnPlayerStatsChanged()
    {
        UpdateWeaponStats();
    }
}
