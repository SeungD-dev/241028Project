using UnityEngine;

public class GrinderProjectile : BaseProjectile
{
    [SerializeField] private float rotationSpeed = 720f;
    public GameObject groundEffectPrefab;

    private float spawnTime;
    private Vector2 targetPosition;
    private float attackRadius;
    private float groundEffectDuration;
    private float damageTickInterval;
    private string groundEffectPoolTag;
    private float airTime;
    private float maxHeight = 3f;

    public override void OnObjectSpawn()
    {
        base.OnObjectSpawn();
        spawnTime = Time.time;

        // ũ�� ������Ʈ
        transform.localScale = Vector3.one * baseProjectileSize;
    }

    public void Initialize(
      float damage,
      Vector2 direction,
      float speed,
      Vector2 targetPos,
      float radius,
      float duration,
      float tickInterval,
      string effectPoolTag,
      float size)
    {
        base.Initialize(damage, direction, speed);

        this.targetPosition = targetPos;
        this.attackRadius = radius;
        this.groundEffectDuration = duration;
        this.damageTickInterval = tickInterval;
        this.groundEffectPoolTag = effectPoolTag;
        this.airTime = Vector2.Distance(transform.position, targetPosition) / speed;

        // ũ�� ����
        transform.localScale = Vector3.one * size;
    }


    protected override void OnTriggerEnter2D(Collider2D other)
    {
        // ����ü ��ü�� ������� ���� �ʵ��� override
    }

    protected override void Update()
    {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        float progress = (Time.time - spawnTime) / airTime;
        if (progress >= 1f)
        {
            CreateGroundEffect();
            ReturnToPool();
            return;
        }

        float height = Mathf.Sin(progress * Mathf.PI) * maxHeight;
        Vector2 currentPos = Vector2.Lerp(startPosition, targetPosition, progress);
        transform.position = new Vector3(currentPos.x, currentPos.y + height, 0);
    }

    private void CreateGroundEffect()
    {
        if (string.IsNullOrEmpty(groundEffectPoolTag))
        {
            Debug.LogError("Ground effect pool tag is not set!");
            return;
        }

        GameObject groundEffect = ObjectPool.Instance.SpawnFromPool(
            groundEffectPoolTag,
            targetPosition,
            Quaternion.identity
        );

        if (groundEffect != null)
        {
            GrinderGroundEffect effect = groundEffect.GetComponent<GrinderGroundEffect>();
            if (effect != null)
            {
                effect.SetPoolTag(groundEffectPoolTag);  // �±� ���� �߰�
                effect.Initialize(damage, attackRadius, groundEffectDuration, damageTickInterval);
                effect.OnObjectSpawn();
            }
        }
    }
}