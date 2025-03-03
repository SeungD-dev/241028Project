using UnityEngine;

public abstract class EnemyAI : MonoBehaviour
{
    public StateMachine stateMachine;
    protected Enemy enemyStats;
    protected Transform playerTransform;
    [HideInInspector] public SpriteRenderer spriteRenderer;
    public Transform PlayerTransform => playerTransform;

    // ���� ����ȭ�� ���� ������
    protected bool isActive;
    protected bool isCulled;  // �ø� ���� ����
    protected Vector3 lastKnownPlayerPosition;

    // �Ÿ� ��� ������Ʈ ����ȭ
    [SerializeField] protected float distanceUpdateThreshold = 15f; // �÷��̾���� �Ÿ��� �� ������ ũ�� ������Ʈ �ֱ� �ø�
    protected float sqrDistanceToPlayer;
    protected float sqrDistanceThreshold;

    // ������ ���� ����
    protected Vector2 moveDirection;
    protected float currentMoveSpeed;

    // �ð��� ȿ�� ���� ����
    protected float effectUpdateInterval = 0.1f;  // �ð��� ȿ�� ������Ʈ �ֱ�
    protected float nextEffectUpdateTime;

    protected virtual void Awake()
    {
        enemyStats = GetComponent<Enemy>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        stateMachine = new StateMachine();

        // �Ÿ� �Ӱ谪 ���� (�Ź� ������ ��� ȸ��)
        sqrDistanceThreshold = distanceUpdateThreshold * distanceUpdateThreshold;

        // ù ȿ�� ������Ʈ �ð� ����
        nextEffectUpdateTime = Time.time + Random.Range(0f, effectUpdateInterval);
    }

    protected virtual void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }
        isActive = GameManager.Instance != null &&
                  GameManager.Instance.currentGameState == GameState.Playing;
        isCulled = false;

        // ���� �ʱ�ȭ
        InitializeStates();
    }

    protected virtual void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    protected virtual void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        isActive = (newState == GameState.Playing);
    }

    protected virtual void InitializeStates()
    {
        var idleState = new IdleState(this);
        var chasingState = new ChasingState(this);

        stateMachine.SetState(idleState);
        stateMachine.AddTransition(idleState, chasingState,
            new FuncPredicate(() => playerTransform != null && IsPlayerAlive() && isActive));
    }

    public virtual void Initialize(Transform target)
    {
        if (target == null) return;

        playerTransform = target;
        lastKnownPlayerPosition = playerTransform.position;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        // �ٷ� ���� ���·� ��ȯ
        var chasingState = new ChasingState(this);
        stateMachine.SetState(chasingState);

        isActive = IsGamePlaying();
    }

    // ������ ������ �ð��� ȿ���� Update���� ó��
    protected virtual void Update()
    {
        // �ø��Ǿ��ų� ������ �Ͻ������Ǿ����� ó������ ����
        if (isCulled || !isActive || playerTransform == null) return;

        // ���� �ӽ� ������Ʈ (�̵� ���� ���� ����)
        stateMachine.Update();

        // �ð��� ȿ�� ������Ʈ (���ѵ� �ֱ��)
        if (Time.time >= nextEffectUpdateTime)
        {
            UpdateVisualEffects();
            nextEffectUpdateTime = Time.time + effectUpdateInterval;
        }
    }

    // ���� �� �̵� ������ FixedUpdate���� ó��
    protected virtual void FixedUpdate()
    {
        // �ø��Ǿ��ų� ������ �Ͻ������Ǿ����� ó������ ����
        if (isCulled || !isActive || playerTransform == null) return;

        // ���� �ӽ� FixedUpdate ȣ��� �̵� ���� ����
        stateMachine.FixedUpdate();
    }

    // �ø� ���� ���� (EnemyCullingManager���� ȣ���)
    public virtual void SetCullingState(bool isVisible)
    {
        isCulled = !isVisible;

        // ������Ʈ Ȱ��ȭ/��Ȱ��ȭ
        enabled = isVisible;

        // Enemy ������Ʈ�� �ø� ���� ����
        if (enemyStats != null)
        {
            enemyStats.SetCullingState(isVisible);
        }
    }

    // �ð��� ȿ�� ������Ʈ (�ٿ, ��ƼŬ ��)
    protected virtual void UpdateVisualEffects()
    {
        // �ٿ ȿ�� ������Ʈ
        if (enemyStats != null && !enemyStats.IsKnockBack)
        {
            enemyStats.UpdateBounceEffect();
        }
    }

    // �÷��̾� ���� üũ
    protected virtual bool IsPlayerAlive()
    {
        return GameManager.Instance != null &&
               GameManager.Instance.PlayerStats != null &&
               GameManager.Instance.PlayerStats.CurrentHealth > 0;
    }

    // ���� ���� üũ
    protected virtual bool IsGamePlaying()
    {
        return GameManager.Instance != null &&
               GameManager.Instance.currentGameState == GameState.Playing;
    }

    // ����׿� �����
    protected virtual void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanceUpdateThreshold);
    }
}