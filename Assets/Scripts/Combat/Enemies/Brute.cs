using UnityEngine;
using DG.Tweening;
using System.Collections;

public class Brute : EnemyAI
{
    [Header("Charge Attack Settings")]
    [SerializeField] private float chargeDetectionRange = 8f; // ���� ���� ����
    [SerializeField] private float chargePrepareTime = 1.2f;  // ���� �غ� �ð�
    [SerializeField] private float chargeSpeed = 15f;         // ���� �ӵ�
    [SerializeField] private float chargeDuration = 0.8f;     // ���� ���� �ð�
    [SerializeField] private float chargeCooldown = 5f;       // ���� ��ٿ� �ð�
    [SerializeField] private Color chargeColor = new Color(0.56f, 0f, 0f); // #8f0000 ����
    [SerializeField] private bool isImmuneToKnockbackWhileCharging = true; // ���� �� �˹� �鿪 ����

    // ���� ���� ����
    private bool isCharging = false;        // ���� ������ ����
    private bool isPreparingCharge = false; // ���� �غ� ������ ����
    private float lastChargeTime = -10f;    // ������ ���� �ð�

    // ĳ�õ� ����
    private Color originalColor;            // ���� ����
    private Vector3 originalScale;          // ���� ũ��
    private Vector2 chargeDirection;        // ���� ����
    private Sequence pulseSequence;         // DOTween ������
    private Rigidbody2D rb;                 // ĳ�õ� ������ٵ�
    private Animator animator;              // �ִϸ����� ������Ʈ

    // �ִϸ��̼� �Ķ���� �̸� (����� ĳ��)
    private const string ANIM_CHARGE = "Brute_Charge";

    // ����ȭ�� ����
    private float sqrChargeDetectionRange;  // ������ ���� ���� ���� (����ȭ��)
    private readonly WaitForSeconds prepareWait; // ĳ�õ� ��� �ð�

    public Brute()
    {
        // ĳ�õ� WaitForSeconds �ʱ�ȭ (����ȭ)
        prepareWait = new WaitForSeconds(chargePrepareTime);
    }

    protected override void Awake()
    {
        base.Awake();

        // ������Ʈ ĳ��
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // ����� ũ�� ĳ��
        originalColor = spriteRenderer ? spriteRenderer.color : Color.white;
        originalScale = transform.localScale;

        // ������ ���� ���� ���� ������ �̸� ���
        sqrChargeDetectionRange = chargeDetectionRange * chargeDetectionRange;
    }

    protected override void InitializeStates()
    {
        base.InitializeStates();

        // ���� ����
        var idleState = new IdleState(this);
        var chasingState = new ChasingState(this);
        var chargeState = new ChargeState(this);
        var prepareChargeState = new PrepareChargeState(this);

        // ���� ��ȯ ����
        stateMachine.SetState(idleState);

        // ��� -> ����: �÷��̾ �����Ǿ��� ��
        stateMachine.AddTransition(idleState, chasingState,
            new FuncPredicate(() => playerTransform != null && IsPlayerAlive() && isActive));

        // ���� -> ���� �غ�: �÷��̾ ���� ���� �ְ� ��ٿ��� �غ�Ǿ��� ��
        stateMachine.AddTransition(chasingState, prepareChargeState,
            new FuncPredicate(() => CanStartCharge()));

        // ���� �غ� -> ����: �غ� �Ϸ�Ǿ��� ��
        stateMachine.AddTransition(prepareChargeState, chargeState,
            new FuncPredicate(() => isPreparingCharge == false && !isCharging));

        // ���� -> ����: ������ �Ϸ�Ǿ��� ��
        stateMachine.AddTransition(chargeState, chasingState,
            new FuncPredicate(() => isCharging == false));
    }

    protected override void Update()
    {
        base.Update();

        // ���� ����ȭ - Ȱ�� �����̰� �̹� ����/�غ� ���� �ƴ� ���� ���� ���� Ȯ��
        if (isActive && !isCulled && !isCharging && !isPreparingCharge &&
            Time.time > lastChargeTime + chargeCooldown && playerTransform != null)
        {
            CheckChargeConditions();
        }
    }

    private void CheckChargeConditions()
    {
        // ������ ���� ���� �Ÿ� ��� (sqrt ���� ȸ��)
        Vector2 toPlayer = playerTransform.position - transform.position;
        float sqrDistanceToPlayer = toPlayer.sqrMagnitude;

        // �÷��̾ ���� ���� ���� �ִ��� Ȯ��
        if (sqrDistanceToPlayer <= sqrChargeDetectionRange)
        {
            // ������ ���� ����ȭ�� ���� ����
            chargeDirection = toPlayer.normalized;

            // ���� �ӽ��� ���� ���� ��ȯ ��û
            // ���� ��ȯ�� InitializeStates���� ������ ���ǹ��� ���� �̷����
        }
    }

    public bool CanStartCharge()
    {
        return playerTransform != null &&
               !isCharging &&
               !isPreparingCharge &&
               Time.time > lastChargeTime + chargeCooldown &&
               (playerTransform.position - transform.position).sqrMagnitude <= sqrChargeDetectionRange;
    }

    public IEnumerator PrepareCharge()
    {
        if (isPreparingCharge || isCharging) yield break;

        isPreparingCharge = true;

        // �غ� ���� �� ���� �÷��̾� ��ġ���� ���� ĳ��
        chargeDirection = (playerTransform.position - transform.position).normalized;

        // �ð��� �ǵ�� - ���� ����
        if (spriteRenderer != null)
        {
            spriteRenderer.color = chargeColor;
        }

        // �ִϸ��̼� ���� - ���� �غ�/���� �ִϸ��̼����� ��ȯ
        if (animator != null)
        {
            animator.Play(ANIM_CHARGE);
        }

        // �ð��� �ǵ�� - DOTween�� ����� ũ�� �Ƶ� ȿ��
        if (pulseSequence != null)
        {
            pulseSequence.Kill();
        }

        pulseSequence = DOTween.Sequence();
        pulseSequence.Append(transform.DOScale(originalScale * 1.2f, chargePrepareTime * 0.4f).SetEase(Ease.OutQuad));
        pulseSequence.Append(transform.DOScale(originalScale, chargePrepareTime * 0.6f).SetEase(Ease.InOutQuad));

        // �غ� �ð� ���
        yield return prepareWait;

        // �غ� �Ϸ�
        isPreparingCharge = false;
    }

    public IEnumerator PerformCharge()
    {
        if (isCharging) yield break;

        isCharging = true;
        lastChargeTime = Time.time;

        // �� ������Ʈ ����
        Enemy enemyComponent = GetComponent<Enemy>();

        // ���� �߿� �˹� �鿪 ����
        if (enemyComponent != null && isImmuneToKnockbackWhileCharging)
        {
            // �˹� �鿪 ���� ����
            enemyComponent.SetKnockbackImmunity(true);
        }

        // �ִϸ��̼��� �̹� PrepareCharge���� Brute_Charge�� �����

        if (rb != null)
        {
            // ���� ��� �̵� ��� (����ȭ��)
            rb.linearVelocity = chargeDirection * chargeSpeed;

            // ���� ���� �ð� ���� ���
            float endTime = Time.time + chargeDuration;
            while (Time.time < endTime && isActive)
            {
                yield return null;
            }

            // �̵� ����
            rb.linearVelocity = Vector2.zero;
        }

        // ���� �ܰ����� ����
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // ���� ũ��� Ȯ���� ����
        transform.localScale = originalScale;

        // �⺻ �ִϸ��̼����� ����
        if (animator != null)
        {
            animator.Play("Brute");
        }

        // �˹� �鿪 ����
        if (enemyComponent != null && isImmuneToKnockbackWhileCharging)
        {
            enemyComponent.SetKnockbackImmunity(false);
        }

        isCharging = false;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        // Ȱ��ȭ�� Ʈ�� ����
        if (pulseSequence != null)
        {
            pulseSequence.Kill();
            pulseSequence = null;
        }

        // �ð��� ���� �ʱ�ȭ
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        transform.localScale = originalScale;

        // �ִϸ��̼� �ʱ�ȭ
        if (animator != null)
        {
            animator.Play("Brute");
        }

        // �˹� �鿪 ���� �ʱ�ȭ
        if (isImmuneToKnockbackWhileCharging)
        {
            Enemy enemyComponent = GetComponent<Enemy>();
            if (enemyComponent != null)
            {
                enemyComponent.SetKnockbackImmunity(false);
            }
        }

        // ���� �÷��� �ʱ�ȭ
        isCharging = false;
        isPreparingCharge = false;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // DOTween ������ ����
        if (pulseSequence != null)
        {
            pulseSequence.Kill();
        }
    }

    #region State Implementations

    // Brute ���� ���� �غ� ����
    public class PrepareChargeState : IState
    {
        private readonly Brute brute;
        private Coroutine prepareCoroutine;

        public PrepareChargeState(Brute brute)
        {
            this.brute = brute;
        }

        public void OnEnter()
        {
            // ���� �غ� �ڷ�ƾ ����
            prepareCoroutine = brute.StartCoroutine(brute.PrepareCharge());
        }

        public void OnExit()
        {
            // �ڷ�ƾ�� ������ ���� ���̶�� ����
            if (prepareCoroutine != null)
            {
                brute.StopCoroutine(prepareCoroutine);
                prepareCoroutine = null;
            }
        }

        public void Update()
        {
            // �غ� �߿��� ������Ʈ�� �ʿ� ����, �ð� ������� ����
        }

        public void FixedUpdate()
        {
            // �ǵ������� ����� - �غ� �߿��� �̵����� ����
        }
    }

    // Brute ���� ���� ����
    public class ChargeState : IState
    {
        private readonly Brute brute;
        private Coroutine chargeCoroutine;

        public ChargeState(Brute brute)
        {
            this.brute = brute;
        }

        public void OnEnter()
        {
            // ���� �ڷ�ƾ ����
            chargeCoroutine = brute.StartCoroutine(brute.PerformCharge());
        }

        public void OnExit()
        {
            // �ڷ�ƾ�� ������ ���� ���̶�� ����
            if (chargeCoroutine != null)
            {
                brute.StopCoroutine(chargeCoroutine);
                chargeCoroutine = null;
            }
        }

        public void Update()
        {
            // ���� �߿��� ������Ʈ�� �ʿ� ����, �ڷ�ƾ���� ó����
        }

        public void FixedUpdate()
        {
            // Rigidbody2D.linearVelocity�� ����ϹǷ� �߰� ���� ������Ʈ �ʿ� ����
        }
    }

    #endregion

    #region Debug Visualization

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // ���� ���� ���� ǥ��
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chargeDetectionRange);
    }

    #endregion
}