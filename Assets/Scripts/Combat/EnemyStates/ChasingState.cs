using UnityEngine;

public class ChasingState : IState
{
    private readonly EnemyAI enemyAI;
    private readonly Transform enemyTransform;
    private Transform playerTransform;
    private readonly Rigidbody2D rb;
    private readonly Enemy enemyStats;
    private readonly SpriteRenderer spriteRenderer;

    // ���� ������ ���� ������
    private Vector2 directionVector = Vector2.zero;
    private Vector2 targetPosition;
    private Vector2 currentPosition;

    // ���� ����ȭ�� ���� ������
    private float moveSpeed;
    private float lastDirectionX;
    private const float DIRECTION_CHANGE_THRESHOLD = 0.05f;

    // Ÿ�̸� ���� ����
    private float nextSpriteFlipTime;
    private float spriteFlipInterval = 0.1f;  // ��������Ʈ �ø� ������Ʈ �ֱ�

    public ChasingState(EnemyAI enemyAI)
    {
        this.enemyAI = enemyAI;
        enemyTransform = enemyAI.transform;
        enemyStats = enemyAI.GetComponent<Enemy>();
        rb = enemyAI.GetComponent<Rigidbody2D>();
        spriteRenderer = enemyAI.spriteRenderer;

        // �÷��̾� ã��
        playerTransform = enemyAI.PlayerTransform;
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    }

    public void OnEnter()
    {
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        // �ʱ�ȭ
        moveSpeed = enemyStats.MoveSpeed;
        nextSpriteFlipTime = Time.time;

        // ���� �ʱ� ���
        if (playerTransform != null)
        {
            CalculateDirection();
        }
    }

    public void OnExit()
    {
        enemyStats.ResetBounceEffect();

        // �̵� ����
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // ������ ������ �ð��� ������Ʈ�� Update���� ó��
    public void Update()
    {
        // ���� ��ȿ�� �˻�
        if (enemyStats.IsKnockBack || playerTransform == null ||
            !IsGamePlaying()) return;

        // ���� ��� (�� ������)
        CalculateDirection();

        // ��������Ʈ �ø��� ������ �ΰ� ������Ʈ
        if (Time.time >= nextSpriteFlipTime)
        {
            UpdateSpriteDirection();
            nextSpriteFlipTime = Time.time + spriteFlipInterval;
        }
    }

    // ���� ��� �̵��� FixedUpdate���� ó��
    public void FixedUpdate()
    {
        // ���� ��ȿ�� �˻�
        if (enemyStats.IsKnockBack || playerTransform == null ||
            !IsGamePlaying()) return;

        // FixedUpdate������ �̹� ���� �������θ� �̵� ����
        ApplyMovement();
    }

    // �÷��̾� ���������� ���� ���
    private void CalculateDirection()
    {
        // ���� ��ġ�� ��� ��ġ
        currentPosition = enemyTransform.position;
        targetPosition = playerTransform.position;

        // ���� ��� (���� ����)
        directionVector.x = targetPosition.x - currentPosition.x;
        directionVector.y = targetPosition.y - currentPosition.y;

        // ����ȭ (���� ���� ����ȭ)
        float sqrMagnitude = directionVector.x * directionVector.x + directionVector.y * directionVector.y;
        if (sqrMagnitude > 0.0001f) // 0���� ������ ���� + �ּ� �̵� �Ӱ谪
        {
            float inverseMagnitude = 1.0f / Mathf.Sqrt(sqrMagnitude);
            directionVector.x *= inverseMagnitude;
            directionVector.y *= inverseMagnitude;
        }
    }

    // ��������Ʈ ���� ������Ʈ (�¿� �ø�)
    private void UpdateSpriteDirection()
    {
        // ������ ����� ����Ǿ��� ���� ��������Ʈ �ø� ������Ʈ
        if (Mathf.Abs(directionVector.x - lastDirectionX) > DIRECTION_CHANGE_THRESHOLD)
        {
            lastDirectionX = directionVector.x;
            if (directionVector.x != 0)
            {
                spriteRenderer.flipX = directionVector.x < 0;
            }
        }
    }

    // ���� �̵� ����
    private void ApplyMovement()
    {
        // Ŀ���� �̵� �ӵ� ��� (�ʿ�� �Ÿ��� ���� �ӵ� ���� ����)
        float appliedSpeed = moveSpeed;

        // ���� ��� �̵� ����
        if (rb != null)
        {
            // ������ٵ� �̵� (Vector2 �������� ������ ���� �ּ�ȭ)
            rb.linearVelocity = new Vector2(
                directionVector.x * appliedSpeed,
                directionVector.y * appliedSpeed
            );
        }
        else
        {
            // Transform ��� �̵��� FixedDeltaTime ���
            enemyTransform.position = new Vector3(
                enemyTransform.position.x + directionVector.x * appliedSpeed * Time.fixedDeltaTime,
                enemyTransform.position.y + directionVector.y * appliedSpeed * Time.fixedDeltaTime,
                enemyTransform.position.z
            );
        }
    }

    // ���� ���� üũ
    private bool IsGamePlaying()
    {
        return GameManager.Instance != null &&
               GameManager.Instance.currentGameState == GameState.Playing;
    }
}