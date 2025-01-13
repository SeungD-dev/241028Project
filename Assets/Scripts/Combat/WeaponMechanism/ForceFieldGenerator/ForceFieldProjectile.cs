using UnityEngine;

public class ForceFieldProjectile : BaseProjectile
{
    private float tickInterval;
    private float lastTickTime;
    private Vector3 originalScale;
    private float actualRadius;  // Force Field Radius �� �����
    private Transform playerTransform;

    protected override void Awake()
    {
        base.Awake();
        originalScale = transform.localScale;
    }

    public void SetTickInterval(float interval)
    {
        tickInterval = interval;
        lastTickTime = Time.time;
    }

    public void SetPlayerTransform(Transform player)
    {
        this.playerTransform = player;
    }


    public void SetForceFieldRadius(float radius)
    {
        this.actualRadius = radius;
        UpdateVisualScale();
    }

    public override void Initialize(
        float damage,
        Vector2 direction,
        float speed,
        float knockbackPower = 0f,
        float range = 10f,
        float projectileSize = 1f,
        bool canPenetrate = false,
        int maxPenetrations = 0,
        float damageDecay = 0.1f)
    {
        base.Initialize(damage, direction, speed, knockbackPower, range, projectileSize);
        // base.Initialize�� ȣ���ϵ�, range�� projectileSize�� ������ ������� ����
    }

    private void UpdateVisualScale()
    {
        // �ð��� ũ�⸦ actualRadius�� 2��� ���� (����)
        transform.localScale = Vector3.one * (actualRadius * 4);
    }
    protected override void Update()
    {
        if (playerTransform == null) return;

        transform.position = playerTransform.position;

        if (Time.time >= lastTickTime + tickInterval)
        {
            ApplyDamageInRange();
            lastTickTime = Time.time;
        }
    }

    private void ApplyDamageInRange()
    {
        //Debug.Log($"Checking for enemies in radius: {actualRadius}");
        // Force Field Radius ������ ���� üũ
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, actualRadius);
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);

                    if (knockbackPower > 0)
                    {
                        Vector2 knockbackDirection = ((Vector2)(enemy.transform.position - transform.position)).normalized;
                        enemy.ApplyKnockback(knockbackDirection * knockbackPower);
                    }
                }
            }
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other) { }
    protected override void HandlePenetration() { }
    protected override void ReturnToPool() { }

    //private void OnDrawGizmos()
    //{
    //    if (!Application.isPlaying) return;

    //    // ���� ���� ������ �ð��� ���� (���� �����ؾ� ��)
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawWireSphere(transform.position, actualRadius);
    //}
}