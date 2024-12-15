using System.Collections.Generic;
using UnityEngine;

public class BladeProjectile : BaseProjectile
{
    private HashSet<Enemy> hitEnemies = new HashSet<Enemy>();
    public override void OnObjectSpawn()
    {
        base.OnObjectSpawn();
        // ������ ������ ���� ��ġ ������Ʈ
        startPosition = transform.position;
        Debug.Log($"OnObjectSpawn - StartPosition set to: {startPosition}, MaxTravelDistance: {maxTravelDistance}");
    }

    protected override void ApplyDamageAndEffects(Enemy enemy)
    {
        // �̹� Ÿ���� ���� ����
        if (hitEnemies.Contains(enemy)) return;

        // ���ο� �� Ÿ��
        hitEnemies.Add(enemy);
        enemy.TakeDamage(damage);

        if (knockbackPower > 0)
        {
            Vector2 knockbackForce = direction * knockbackPower;
            enemy.ApplyKnockback(knockbackForce);
        }

        HandlePenetration();
    }

    protected override void Update()
    {
        float distanceFromStart = Vector2.Distance(startPosition, transform.position);

        // �� �����Ӹ��� ������ Ȯ��
        Debug.Log($"Update - Current Position: {transform.position}, StartPosition: {startPosition}, " +
                  $"Distance: {distanceFromStart}, MaxDistance: {maxTravelDistance}, " +
                  $"Speed: {speed}, Direction: {direction}");

        // ����ü �̵�
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // �ִ� ��Ÿ� ���� �� Ǯ�� ��ȯ
        if (distanceFromStart >= maxTravelDistance)
        {
            ReturnToPool();
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        hitEnemies.Clear();
    }
}