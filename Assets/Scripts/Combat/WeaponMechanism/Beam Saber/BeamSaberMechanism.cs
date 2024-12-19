using System.Collections;
using UnityEngine;
public class BeamSaberMechanism : WeaponMechanism
{
    private bool isSecondAttackReady = false;
    private float secondAttackDelay = 0.25f;
    private LayerMask enemyLayer;
    private MonoBehaviour ownerComponent;
    private bool isAttacking = false;  // ���� ���� ������ Ȯ���ϴ� �÷���

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        enemyLayer = LayerMask.GetMask("Enemy");
        ownerComponent = player.GetComponent<MonoBehaviour>();
    }

    protected override void Attack(Transform target)
    {
        if (isAttacking) return;  // �̹� ���� ���̸� ���ο� ���� ����
        isAttacking = true;
        SpawnCircularAttack();
        ownerComponent. StartCoroutine(ResetAttackState());

        if (weaponData.currentTier >= 3 && !isSecondAttackReady)
        {
            isSecondAttackReady = true;
            ownerComponent.StartCoroutine(PerformSecondAttack());
        }
    }

    private IEnumerator ResetAttackState()
    {
        // �ִϸ��̼��� ������ ���� ������ ��� (25������)
        yield return new WaitForSeconds(25f / 60f);
        isAttacking = false;
    }

    // UpdateMechanism�� �������̵��Ͽ� �� ���� ���� �����ϵ��� ����
    public override void UpdateMechanism()
    {
        if (Time.time >= lastAttackTime + currentAttackDelay)
        {
            Attack(null);  // null�� �����ص� ������ �����
            lastAttackTime = Time.time;
        }
    }

    private void SpawnCircularAttack()
    {
        // ������Ʈ ����
        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            playerTransform.position,
            Quaternion.identity
        );

        // ������Ʈ�� ��Ȱ��ȭ ���¶�� Ȱ��ȭ
        if (!projectileObj.activeSelf)
        {
            Debug.LogWarning("BeamSaber projectile was inactive after spawn, activating...");
            projectileObj.SetActive(true);
        }

        BeamSaberProjectile projectile = projectileObj.GetComponent<BeamSaberProjectile>();
        if (projectile != null)
        {
            // �ʱ�ȭ ���� ����
            projectile.SetPoolTag(poolTag);  // Ǯ �±� ���� ����
            projectile.SetupCircularAttack(  // �� ���� ���� ����
                weaponData.CalculateFinalRange(playerStats),
                enemyLayer,
                playerTransform
            );

            // ���������� ������ ���� �ʱ�ȭ
            projectile.Initialize(
                weaponData.CalculateFinalDamage(playerStats),
                Vector2.zero,
                0f,
                weaponData.CalculateFinalKnockback(playerStats),
                weaponData.CalculateFinalRange(playerStats),
                weaponData.CalculateFinalProjectileSize(playerStats)
            );
        }
        else
        {
            Debug.LogError("BeamSaberProjectile component not found!");
        }
    }
    private IEnumerator PerformSecondAttack()
    {
        yield return new WaitForSeconds(secondAttackDelay);
        SpawnCircularAttack();
        isSecondAttackReady = false;
    }
}