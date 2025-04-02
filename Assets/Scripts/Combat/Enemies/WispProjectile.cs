using UnityEngine;
using DG.Tweening;
using System.Collections;
public enum ProjectileState
{
    Preparing,  // �غ� �� (�Ӹ� ���� �������� ��)
    Launched,   // �߻�� (�÷��̾� �������� ���ư��� ��)
    Destroyed   // �ı��� (Ǯ�� ��ȯ)
}
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class WispProjectile : MonoBehaviour, IPooledObject
{
    [Header("����ü ����")]
    [SerializeField] private float lifetime = 5f;       // ����ü ����
    [SerializeField] private float damage = 10f;        // ����ü ������
    [SerializeField] private LayerMask targetLayers;    // Ÿ�� ���̾� (�÷��̾�)
    [SerializeField] private Sprite[] projectileSprites;
    [SerializeField] private float blinkInterval = 0.15f;
    private float nextBlinkTime;
    private int currentSpriteIndex = 0;

    //������Ƽ
    private ProjectileState currentState = ProjectileState.Preparing;

    // ���� üũ�� �Ӽ� �߰�
    public bool IsLaunched => currentState == ProjectileState.Launched;
    public bool IsPreparing => currentState == ProjectileState.Preparing;
    // ������Ʈ ĳ��
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private Transform ownerTransform;
    // ĳ�̵� ������Ʈ ������ (�ܺο��� GetComponent ȣ�� ���� ���)
    public SpriteRenderer GetSpriteRenderer() => spriteRenderer;

    // �ð��� ȿ��
    private Sequence colorSequence;
    private Vector3 originalScale;

    // �̵� ����ȭ�� ���� ����
    private Vector2 direction;
    private float speed;
    private bool isActive = false;

    // Ǯ���� ���� ����
    private string poolTag = "Wisp_Projectile";
    private WaitForSeconds lifetimeWait;
    private Coroutine lifetimeCoroutine;

    private void Awake()
    {
        // ������Ʈ ĳ��
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            circleCollider = gameObject.AddComponent<CircleCollider2D>();
        }

        // ������ٵ� ����
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // �浹ü ����
        circleCollider.isTrigger = true;
        circleCollider.radius = 0.3f;

        // ���� ũ�� ����
        originalScale = transform.localScale;

        // ĳ�õ� WaitForSeconds
        lifetimeWait = new WaitForSeconds(lifetime);
    }
    public void SetOwner(Transform owner)
    {
        ownerTransform = owner;
    }
    private void Update()
    {
        // ����ü�� Ȱ��ȭ�� ���¿����� ������ ó��
        if (Time.time > nextBlinkTime)
        {
            // ��������Ʈ �ε��� ��ȯ (0 -> 1, 1 -> 0)
            currentSpriteIndex = 1 - currentSpriteIndex;

            if (spriteRenderer != null && projectileSprites != null && projectileSprites.Length > 1)
            {
                spriteRenderer.sprite = projectileSprites[currentSpriteIndex];
            }

            // ���� ������ �ð� ����
            nextBlinkTime = Time.time + blinkInterval;
        }

        // �غ� ������ ���� ������ üũ
        if (currentState == ProjectileState.Preparing && ownerTransform != null)
        {
            // ������(Wisp)�� ��Ȱ��ȭ�Ǿ����� Ȯ��
            if (!ownerTransform.gameObject.activeInHierarchy)
            {
                // �����ڰ� ��������Ƿ� ����ü�� Ǯ�� ��ȯ
                ReturnToPool();
            }
        }
    }

    public void OnObjectSpawn()
    {
        // ���� �ʱ�ȭ
        currentState = ProjectileState.Preparing;

        // ����ü Ȱ��ȭ �� �ʱ�ȭ
        isActive = false; // �߻�Ǳ� �������� false
        transform.localScale = originalScale;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // ���� �ڷ�ƾ ����
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
        }
        lifetimeCoroutine = StartCoroutine(LifetimeCountdown());
    }

    // ����ü �߻� �޼���
    public void Launch(Vector2 direction, float speed)
    {
        this.direction = direction.normalized;
        this.speed = speed;

        // ���� ����
        currentState = ProjectileState.Launched;
        isActive = true;

        // �߻� �������� ȸ�� (������)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // ���� ��� �̵� ����
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        // �߻� �� �ణ�� ������ ȿ��
        transform.DOScale(originalScale * 1.2f, 0.2f).SetEase(Ease.OutBack).OnComplete(() => {
            transform.DOScale(originalScale, 0.2f);
        });

        // ���� ������ ȿ���� ������ �⺻ ����
        if (colorSequence == null && spriteRenderer != null)
        {
            Color startColor = spriteRenderer.color;
            Color targetColor = new Color(0.56f, 0f, 0f); // #8f0000

            colorSequence = DOTween.Sequence();
            colorSequence.Append(spriteRenderer.DOColor(targetColor, 0.3f).SetLoops(-1, LoopType.Yoyo));
        }
    }
    public void ProjectileLaunched()
    {
        // ���� Preparing ������ ���� ���� ����
        if (currentState == ProjectileState.Preparing)
        {
            currentState = ProjectileState.Launched;
            isActive = true;
        }
    }

    // ���� ������ ���� (�ܺο��� ȣ��)
    public void SetColorSequence(Sequence sequence)
    {
        // ���� ������ ����
        if (colorSequence != null)
        {
            colorSequence.Kill();
        }

        colorSequence = sequence;
    }

    // �ʱ� ���� ���� (�ܺο��� ȣ��)
    public void SetInitialColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    // ���� ������ ���� (�ܺο��� ȣ��)
    public void StartColorBlink(float interval)
    {
        // ���� ������ ����
        if (colorSequence != null)
        {
            colorSequence.Kill();
        }

        if (spriteRenderer != null)
        {
            colorSequence = DOTween.Sequence();
            colorSequence.Append(spriteRenderer.DOColor(Color.white, interval).SetLoops(-1, LoopType.Yoyo));
        }
    }

    // ���� ī��Ʈ�ٿ� �ڷ�ƾ
    private IEnumerator LifetimeCountdown()
    {
        yield return lifetimeWait;

        // ������ ���ϸ� Ǯ�� ��ȯ
        ReturnToPool();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ÿ�� ���̾�� �浹 üũ
        if (((1 << other.gameObject.layer) & targetLayers) != 0)
        {
            // �÷��̾�� ������
            PlayerStats playerStats = other.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(damage);
            }

            // �浹 ȿ�� �� Ǯ�� ��ȯ
            PlayHitEffect();
            ReturnToPool();
        }
        else if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            // ���̳� ��ֹ��� �浹 ��
            PlayHitEffect();
            ReturnToPool();
        }
    }

    // �浹 ȿ�� ���
    private void PlayHitEffect()
    {
        // ��Ʈ ȿ���� ��ƼŬ �ý����� ������ ���
        // ���⼭�� ������ ������ ȿ���� ����
        transform.DOScale(originalScale * 0.1f, 0.2f).SetEase(Ease.InBack);

        // ���� ���̵� �ƿ�
        if (spriteRenderer != null)
        {
            spriteRenderer.DOFade(0f, 0.2f);
        }
    }

    // Ǯ�� ��ȯ
    private void ReturnToPool()
    {
        // ���� ����
        currentState = ProjectileState.Destroyed;
        isActive = false;

        // ���� �ʱ�ȭ
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        // ������ �ʱ�ȭ
        transform.localScale = originalScale;

        // ������ ����
        if (colorSequence != null)
        {
            colorSequence.Kill();
            colorSequence = null;
        }

        // �ӵ� �ʱ�ȭ
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // ������Ʈ Ǯ�� ��ȯ
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // ��Ȱ��ȭ �� ����
        if (colorSequence != null)
        {
            colorSequence.Kill();
            colorSequence = null;
        }

        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }

        // Ȱ�� ���� ����
        isActive = false;
    }

    // ��ü �ı� �� ����
    private void OnDestroy()
    {
        if (colorSequence != null)
        {
            colorSequence.Kill();
        }
    }
}