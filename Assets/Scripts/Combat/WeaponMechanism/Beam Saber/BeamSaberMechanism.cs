using System.Collections;
using UnityEngine;

public class BeamSaberMechanism : WeaponMechanism
{
    private bool isSecondAttackReady = false;
    private float secondAttackDelay = 0.25f;
    private LayerMask enemyLayer;
    private MonoBehaviour ownerComponent;
    private float attackCooldown = 0f;
    private bool isComboAttack = false;
    private int comboCount = 0;
    private const int MAX_COMBO = 2;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        enemyLayer = LayerMask.GetMask("Enemy");
        ownerComponent = player.GetComponent<MonoBehaviour>();

        if (ownerComponent == null)
        {
            Debug.LogError("Failed to get MonoBehaviour component from player!");
            return;
        }

        // ���� �ڷ�ƾ ����
        if (isComboAttack)
        {
            ownerComponent.StopCoroutine(ComboAttackSequence());
            isComboAttack = false;
            comboCount = 0;
        }
    }

    public override void UpdateMechanism()
    {
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
            return;
        }

        if (Time.time >= lastAttackTime + currentAttackDelay)
        {
            Attack(null);
            lastAttackTime = Time.time;

            // 3Ƽ�� �̻��� �� �޺� ���� Ȱ��ȭ
            if (weaponData.currentTier >= 3)
            {
                if (!isComboAttack)
                {
                    isComboAttack = true;
                    comboCount = 1;
                    ownerComponent.StartCoroutine(ComboAttackSequence());
                }
            }
            else
            {
                attackCooldown = currentAttackDelay * 0.8f; // �⺻ ��ٿ�
            }
        }
    }

    private IEnumerator ComboAttackSequence()
    {
        while (comboCount < MAX_COMBO)
        {
            yield return new WaitForSeconds(secondAttackDelay);
            if (ownerComponent == null || !ownerComponent.gameObject.activeInHierarchy) yield break; // ���� üũ

            SpawnCircularAttack();
            comboCount++;
        }

        // �޺� ���� �� ��ٿ� ����
        attackCooldown = currentAttackDelay * 0.8f;
        isComboAttack = false;
        comboCount = 0;
    }

    protected override void Attack(Transform target)
    {
        if (ownerComponent != null && ownerComponent.gameObject.activeInHierarchy)
        {
            SpawnCircularAttack();
        }
        else
        {
            Debug.LogWarning("Cannot perform attack: owner component is null or inactive");
            // ���� ���н� �޺� ���� ����
            isComboAttack = false;
            comboCount = 0;
        }
    }

    private void SpawnCircularAttack()
    {
        if (ObjectPool.Instance == null || ownerComponent == null) return;

        try
        {
            if (!ownerComponent.gameObject.activeInHierarchy) return;

            Vector3 spawnPosition = playerTransform.position;
            GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(poolTag, spawnPosition, Quaternion.identity);

            if (projectileObj == null)
            {
                Debug.LogWarning("Failed to spawn BeamSaber projectile from pool");
                return;
            }

            BeamSaberProjectile projectile = projectileObj.GetComponent<BeamSaberProjectile>();
            if (projectile == null)
            {
                Debug.LogError("BeamSaber projectile is missing BeamSaberProjectile component");
                return;
            }

            projectile.SetPoolTag(poolTag);

            // ���� ���
            float finalDamage = weaponData.CalculateFinalDamage(playerStats);
            float finalKnockback = weaponData.CalculateFinalKnockback(playerStats);
            float finalRange = weaponData.CalculateFinalRange(playerStats);
            float finalSize = weaponData.CalculateFinalProjectileSize(playerStats);  // ���� �������� ũ�� ���� ���

            if (isComboAttack && comboCount > 1)
            {
                finalDamage *= 1.2f;
            }

            // ������Ÿ�� �ʱ�ȭ �� ũ�� ������ �Բ� ����
            projectile.Initialize(finalDamage, Vector2.zero, 0f, finalKnockback, finalRange, finalSize);
            projectile.SetupCircularAttack(finalRange, enemyLayer, playerTransform);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error spawning BeamSaber projectile: {e.Message}");
        }
    }
    public override void OnPlayerStatsChanged()
    {
        base.OnPlayerStatsChanged();
        // �ʿ��� ��� �߰����� ���� ������Ʈ ����
    }
}