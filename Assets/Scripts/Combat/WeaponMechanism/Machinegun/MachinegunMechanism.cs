using UnityEngine;

public class MachinegunMechanism : WeaponMechanism
{
    private float spreadAngle = 30f; // ź���� ���� (�������� ��15��)

    protected override void Attack(Transform target)
    {
        if (target == null) return;

        // �⺻ �߻� ���� ���
        Vector2 baseDirection = (target.position - playerTransform.position).normalized;

        // ������ ź���� ���� ����
        float randomSpread = Random.Range(-spreadAngle, spreadAngle);

        // ź������ ����� ���� �߻� ���� ���
        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
        float finalAngle = baseAngle + randomSpread;

        // ���� ������ Vector2�� ��ȯ
        Vector2 spreadDirection = new Vector2(
            Mathf.Cos(finalAngle * Mathf.Deg2Rad),
            Mathf.Sin(finalAngle * Mathf.Deg2Rad)
        ).normalized;

        FireProjectileWithSpread(target, spreadDirection);
    }

    private void FireProjectileWithSpread(Transform target, Vector2 spreadDirection)
    {
        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            playerTransform.position,
            Quaternion.identity
        );

        MachinegunProjectile projectile = projectileObj.GetComponent<MachinegunProjectile>();
        if (projectile != null)
        {
            float damage = weaponData.CalculateFinalDamage(playerStats);
            float knockbackPower = weaponData.CalculateFinalKnockback(playerStats);
            float projectileSpeed = weaponData.CurrentTierStats.projectileSpeed;
            float projectileSize = weaponData.CalculateFinalProjectileSize(playerStats);

            projectile.SetPoolTag(poolTag);
            projectile.Initialize(
                damage,
                spreadDirection,
                projectileSpeed,
                knockbackPower,
                currentRange,
                projectileSize
            );
        }
    }
    protected override void InitializeProjectilePool()
    {
        poolTag = $"{weaponData.weaponType}Projectile";
        if (weaponData.projectilePrefab != null)
        {
            // Ǯ ũ�⸦ �� ũ�� ����
            ObjectPool.Instance.CreatePool(poolTag, weaponData.projectilePrefab, 20);
            Debug.Log($"Created projectile pool with tag: {poolTag}");
        }
        else
        {
            Debug.LogError($"Projectile prefab is missing for weapon: {weaponData.weaponName}");
        }
    }
}