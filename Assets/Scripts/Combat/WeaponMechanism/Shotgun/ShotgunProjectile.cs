using UnityEngine;

public class ShotgunProjectile : BaseProjectile
{
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                ApplyDamageAndEffects(enemy);
                // ���� ����ü�� �������� �����Ƿ� ��� Ǯ�� ��ȯ
                ReturnToPool();
            }
        }
    }
}
