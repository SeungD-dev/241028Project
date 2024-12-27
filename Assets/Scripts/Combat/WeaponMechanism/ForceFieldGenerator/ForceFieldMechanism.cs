using UnityEngine;

public class ForceFieldMechanism : WeaponMechanism
{
    private ForceFieldProjectile currentForceField;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        CreateForceField();
    }

    public void Cleanup()
    {
        if (currentForceField != null)
        {
            Object.Destroy(currentForceField.gameObject);
            currentForceField = null;
        }
    }
    private void CreateForceField()
    {
        if (weaponData.projectilePrefab != null)
        {
            GameObject forceFieldObj = Object.Instantiate(weaponData.projectilePrefab, playerTransform.position, Quaternion.identity);
            currentForceField = forceFieldObj.GetComponent<ForceFieldProjectile>();

            if (currentForceField != null)
            {
                float damage = weaponData.CalculateFinalDamage(playerStats);
                float knockbackPower = weaponData.CalculateFinalKnockback(playerStats);

                // �⺻ �ʱ�ȭ
                currentForceField.Initialize(
                    damage,
                    Vector2.zero,
                    0f,
                    knockbackPower,
                    1f,  // range�� ���õ�
                    1f   // projectileSize�� ���õ�
                );

                // ForceField ���� ����
                currentForceField.SetTickInterval(weaponData.CurrentTierStats.forceFieldTickInterval);
                currentForceField.SetPlayerTransform(playerTransform);
                currentForceField.SetForceFieldRadius(weaponData.CurrentTierStats.forceFieldRadius);
            }
        }
        else
        {
            Debug.LogError($"Force Field prefab is missing for weapon: {weaponData.weaponName}");
        }
    }

    public override void UpdateMechanism()
    {
        // �����ʵ�� Update���� ��ü������ ����
    }

    // Attack�� ������� ����
    protected override void Attack(Transform target) { }

    public override void OnPlayerStatsChanged()
    {
        base.OnPlayerStatsChanged();

        if (currentForceField != null)
        {
            float damage = weaponData.CalculateFinalDamage(playerStats);
            float knockbackPower = weaponData.CalculateFinalKnockback(playerStats);

            currentForceField.Initialize(
                damage,
                Vector2.zero,
                0f,
                knockbackPower,
                1f,
                1f
            );
            currentForceField.SetForceFieldRadius(weaponData.CurrentTierStats.forceFieldRadius);
        }
    }
}