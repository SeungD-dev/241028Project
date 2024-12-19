using UnityEngine;

public class BeamSaberProjectile : BaseProjectile
{
    private float attackRadius;
    private LayerMask enemyLayer;
    private Animator animator;
    private static readonly int BaseLayerIndex = 0;
    private SpriteRenderer spriteRenderer;
    private static int instanceCounter = 0;
    private int instanceId;
    private Transform playerTransform;

    private float elapsedTime = 0f;
    private bool attackExecuted = false;
    private const float FRAME_TIME = 1f / 60f;
    private const int ATTACK_START_FRAME = 10;
    private const int ATTACK_END_FRAME = 15;
    private const int TOTAL_FRAMES = 25;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        instanceId = ++instanceCounter;
    }

    public void SetupCircularAttack(float radius, LayerMask enemyMask, Transform player)
    {
        // ������Ʈ�� ��Ȱ��ȭ ���¶�� Ȱ��ȭ
        if (!gameObject.activeSelf)
        {
            Debug.LogWarning($"BeamSaber #{instanceId} was inactive during SetupCircularAttack!");
            gameObject.SetActive(true);
        }

        attackRadius = radius;
        enemyLayer = enemyMask;
        playerTransform = player;

        // ���� ������ sprite ũ�� ����ȭ
        UpdateVisualSize();
    }

    private void UpdateVisualSize()
    {
        if (spriteRenderer == null || !spriteRenderer.sprite) return;

        // ��������Ʈ�� ���� ũ�� ���
        float spriteSize = spriteRenderer.sprite.bounds.size.x;

        if (spriteSize > 0)
        {
            // attackRadius�� ���� ������� �ʰ� range ���� �״�� ���
            transform.localScale = Vector3.one;  // ���� �⺻ �����Ϸ� ����

            // Range�� ProjectileSize�� 1:1�� ��Ī�ǵ��� ������ ����
            float targetScale = (attackRadius * 2);  // Range�� ���� �������̹Ƿ� �������� ��ȯ
            transform.localScale = Vector3.one * targetScale;
        }
    }
    public override void OnObjectSpawn()
    {
        base.OnObjectSpawn();

        if (!gameObject.activeSelf)
        {
            Debug.LogWarning($"BeamSaber #{instanceId} was inactive during OnObjectSpawn!");
            gameObject.SetActive(true);
        }
        ResetState();
    }
    private void ResetState()
    {
        elapsedTime = 0f;
        attackExecuted = false;

        if (animator != null && gameObject.activeSelf)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.Play("Beamsaber_Atk", BaseLayerIndex, 0f);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Debug.Log("BeamSaber OnEnable called");
        if (animator != null)
        {
            animator.enabled = true;
        }
    }
  

    protected override void Update()
    {
        if (playerTransform != null)
        {
            // �÷��̾�� ���� �浹 ���� üũ
            bool isPlayerCollidingWithEnemy = Physics2D.OverlapCircle(playerTransform.position, 0.1f, enemyLayer);
            if (isPlayerCollidingWithEnemy)
            {
                Debug.Log($"BeamSaber #{instanceId} - Player is colliding with enemy");
            }

            transform.position = playerTransform.position;
        }

        // �÷��̾� ��ġ ����
        if (playerTransform != null)
        {
            transform.position = playerTransform.position;
        }

        elapsedTime += Time.deltaTime;
        int currentFrame = Mathf.FloorToInt(elapsedTime / FRAME_TIME);

        // ���� ������ üũ
        if (!attackExecuted &&
            currentFrame >= ATTACK_START_FRAME &&
            currentFrame <= ATTACK_END_FRAME)
        {
            PerformCircularAttack();
            attackExecuted = true;
        }

        // �ִϸ��̼� �Ϸ� üũ
        if (currentFrame >= TOTAL_FRAMES)
        {
            ReturnToPool();
        }
    }
    private void PerformCircularAttack()
    {
        if (!gameObject.activeSelf) return;  // ��Ȱ��ȭ ���¸� �������� ����

        // ���� �� �� Ž��
        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            transform.position,
            attackRadius,
            enemyLayer
        );

        // ���� �����Ǿ��� ��쿡�� �������� �˹� ó��
        if (enemies.Length > 0)
        {
            foreach (Collider2D enemyCollider in enemies)
            {
                if (!gameObject.activeSelf) break;  // ���߿� ��Ȱ��ȭ�Ǹ� �ߴ�

                Enemy enemy = enemyCollider.GetComponent<Enemy>();
                if (enemy != null && enemy.gameObject.activeSelf)  // ���� Ȱ��ȭ ������ ����
                {
                    enemy.TakeDamage(damage);

                    if (knockbackPower > 0)
                    {
                        Vector2 knockbackDirection = (enemy.transform.position - transform.position).normalized;
                        enemy.ApplyKnockback(knockbackDirection * knockbackPower);
                    }
                }
            }
        }
    }
    protected override void OnDisable()
    {
        Debug.Log($"BeamSaber #{instanceId} OnDisable - Player position: {(playerTransform != null ? playerTransform.position.ToString() : "null")}");
        base.OnDisable();
        attackExecuted = false;
        elapsedTime = 0f;

        if (animator != null)
        {
            animator.enabled = false;
        }
    }
    private void OnDrawGizmos()
    {// ���� ���� ǥ�� (������)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        // ���� ��������Ʈ ũ�� ǥ�� (�����)
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Gizmos.color = Color.yellow;
            float size = transform.localScale.x / 2f;  // ���� ǥ�õǴ� ũ���� ������
            Gizmos.DrawWireSphere(transform.position, size);
        }
    }

}