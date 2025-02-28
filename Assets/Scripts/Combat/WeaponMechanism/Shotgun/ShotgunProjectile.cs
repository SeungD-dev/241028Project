using UnityEngine;

public class ShotgunProjectile : BulletProjectile
{
    // ����ü ���� ����
    private int processingState = 0; // 0: �ʱ�, 1: Ȱ��ȭ, 2: �浹 ��, 3: ��ȯ ��

    protected override void OnEnable()
    {
        base.OnEnable();
        processingState = 1;  // Ȱ�� ���·� ����
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        // �̹� ó�� ���̸� ����
        if (processingState != 1) return;

        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                processingState = 2;  // �浹 ó�� ��
                ApplyDamageAndEffects(enemy);
                SpawnDestroyVFX();

                processingState = 3;  // ��ȯ ��
                ReturnToPool();
            }
        }
    }

    protected override void Update()
    {
        // Ȱ�� ������ ���� ������Ʈ ó��
        if (processingState != 1) return;

        // �̵� ó��
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // �ִ� ��Ÿ� Ȯ��
        if (Vector2.Distance(startPosition, transform.position) >= maxTravelDistance)
        {
            processingState = 3;  // ��ȯ ��
            SpawnDestroyVFX();
            ReturnToPool();
        }
    }

    protected override void ReturnToPool()
    {
        // �̹� ��ȯ ������ Ȯ��
        if (processingState == 3 && !string.IsNullOrEmpty(poolTag))
        {
            // �����ϰ� Ǯ�� ��ȯ
            ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            // Ǯ �±װ� ������ ��Ȱ��ȭ
            gameObject.SetActive(false);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        processingState = 0;  // �ʱ� ���·� ����
    }
}