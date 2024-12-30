using UnityEngine;

public class BulletProjectile : BaseProjectile
{
    protected void SpawnDestroyVFX()
    {
        GameObject vfx = ObjectPool.Instance.SpawnFromPool("Bullet_DestroyVFX", transform.position, transform.rotation);
        if (vfx != null)
        {
            BulletDestroyVFX destroyVFX = vfx.GetComponent<BulletDestroyVFX>();
            if (destroyVFX != null)
            {
                destroyVFX.SetPoolTag("Bullet_DestroyVFX");

                // ���� ����ü�� ���� ũ�⸦ ����
                Vector3 currentProjectileScale = transform.localScale;

                // projectileSize�� �ִٸ� �װ͵� ��� (BaseProjectile�� �ִٸ�)
                if (baseProjectileSize > 0)
                {
                    currentProjectileScale *= baseProjectileSize;
                }

                destroyVFX.SetEffectScale(currentProjectileScale);
            }
        }
    }
}
