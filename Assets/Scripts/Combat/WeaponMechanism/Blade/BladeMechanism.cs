using UnityEngine;

public class BladeMechanism : WeaponMechanism
{
    private Vector2 targetDirection;
    private Vector2 spawnPosition;

    protected override void InitializeProjectilePool()
    {
        // Ǯ �±� ���� �� ����
        poolTag = $"{weaponData.weaponType}Projectile";
        

        if (weaponData.projectilePrefab != null)
        {
            ObjectPool.Instance.CreatePool(poolTag, weaponData.projectilePrefab, 10);
        }      
    }

    protected override void Attack(Transform target)
    {
        if (target == null) return;

        SoundManager.Instance.PlaySound("Slash_sfx", 1f, false);

        if (string.IsNullOrEmpty(poolTag))
        {
            InitializeProjectilePool();
        }

        // ���� ��� ����ȭ
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

        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            spawnPosition,
            Quaternion.identity
        );

        if (projectileObj != null && projectileObj.TryGetComponent(out BladeProjectile projectile))
        {
            projectile.SetPoolTag(poolTag);
            projectile.Initialize(
                weaponData.CalculateFinalDamage(playerStats),
                targetDirection,
                weaponData.CurrentTierStats.projectileSpeed,
                weaponData.CalculateFinalKnockback(playerStats),
                weaponData.CalculateFinalRange(playerStats),
                weaponData.CalculateFinalProjectileSize(playerStats),
                true,
                0,
                0f
            );
        }
    }
}