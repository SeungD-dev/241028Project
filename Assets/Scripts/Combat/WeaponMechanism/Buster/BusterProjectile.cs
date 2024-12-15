using UnityEngine;

public class BusterProjectile : BaseProjectile
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
            ReturnToPool();
        }
        else
        {
            HandlePenetration();
        }
    }
}