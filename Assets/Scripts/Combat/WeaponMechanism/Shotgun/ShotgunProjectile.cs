using UnityEngine;

public class ShotgunProjectile : BulletProjectile
{
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                ApplyDamageAndEffects(enemy);
                SpawnDestroyVFX();  // �Ҹ� ȿ�� �߰�
                ReturnToPool();
            }
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
