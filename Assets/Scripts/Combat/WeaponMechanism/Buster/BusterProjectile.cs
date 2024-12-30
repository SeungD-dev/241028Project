using UnityEngine;

public class BusterProjectile : BulletProjectile
{
    protected override void ApplyDamageAndEffects(Enemy enemy)
    {
        enemy.TakeDamage(damage);
        if (knockbackPower > 0)
        {
            Vector2 knockbackForce = direction * knockbackPower;
            enemy.ApplyKnockback(knockbackForce);
        }

        // 3Ƽ�� �̻��� ��� ����, �ƴ� ��� ��� ����
        if (!canPenetrate)
        {
            SpawnDestroyVFX();  // �Ҹ� ȿ�� �߰�
            ReturnToPool();
        }
        else
        {
            HandlePenetration();
        }
    }

    protected override void Update()
    {
        base.Update();

        // �ִ� ��Ÿ� ���� �� �Ҹ� ȿ�� �߰�
        if (Vector2.Distance(startPosition, transform.position) >= maxTravelDistance)
        {
            SpawnDestroyVFX();
            ReturnToPool();
        }
    }
}