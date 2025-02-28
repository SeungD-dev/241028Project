using UnityEngine;

public class Hunter : EnemyAI
{
    [Header("Aura Settings")]
    [SerializeField] private Transform auraTransform;  // ���� ������Ʈ�� Transform
    [SerializeField] private float rotationSpeed = 100f;  // ȸ�� �ӵ� (��/��)

    // ���� ȸ�� ����ȭ�� ���� ����
    [SerializeField] private float auraRotationInterval = 0.033f; // �� 30Hz�� ȸ�� ������Ʈ
    private float nextAuraRotationTime;
    private float accumulatedRotation; // ������ ȸ����

    // �Ÿ� ��� ����ȭ�� ���� �߰� ����
    [SerializeField] private float auraOptimizationDistance = 20f; // ����ȭ ���� �Ÿ�
    private float sqrAuraOptimizationDistance;
    private bool isAuraOptimized = false;

    protected override void Awake()
    {
        base.Awake();

        // �ʱ�ȭ
        nextAuraRotationTime = Time.time;
        sqrAuraOptimizationDistance = auraOptimizationDistance * auraOptimizationDistance;

        // �ʱ� ȸ�� ���� �����ϰ� �����Ͽ� ��� ���� ������ ��ġ���� �������� �ʵ��� ��
        if (auraTransform != null)
        {
            auraTransform.Rotate(Vector3.forward * Random.Range(0f, 360f));
        }
    }

    protected override void InitializeStates()
    {
        base.InitializeStates();
        // Hunter ���� ���� �߰� ����
    }

    protected override void Update()
    {
        // �⺻ AI ���� ������Ʈ (�̵� ����)
        base.Update();

        // �ø��Ǿ��ų� ��Ȱ�� ���¸� ���� ������Ʈ �ǳʶٱ�
        if (isCulled || !isActive) return;

        // ���� ȸ�� - �ð��� ������Ʈ�̹Ƿ� Update���� ����
        UpdateAura();
    }

    protected override void FixedUpdate()
    {
        // �⺻ AI ���� ���� ������Ʈ
        base.FixedUpdate();
    }

    // �Ÿ��� ���� ���� ȿ�� ����ȭ ������Ʈ
    protected override void UpdateVisualEffects()
    {
        base.UpdateVisualEffects();

        // �÷��̾���� �Ÿ��� ���� ���� ����ȭ ���� ����
        if (playerTransform != null)
        {
            float sqrDistance = (transform.position - playerTransform.position).sqrMagnitude;

            // �Ÿ��� ���� ���� ����ȭ ���� ����
            if (sqrDistance > sqrAuraOptimizationDistance)
            {
                // �ָ� ���� �� ���� ����ȭ (���� ������ ����Ʈ�� ������Ʈ)
                isAuraOptimized = true;
                auraRotationInterval = 0.1f; // 10Hz�� ����
            }
            else
            {
                // ������ ���� �� ���� ������Ʈ
                isAuraOptimized = false;
                auraRotationInterval = 0.033f; // 30Hz�� ����
            }
        }
    }

    // ���� ������Ʈ �޼���
    private void UpdateAura()
    {
        if (auraTransform == null) return;

        // �ð� ��� ������Ʈ
        if (Time.time >= nextAuraRotationTime)
        {
            // ������ ������Ʈ ���� ����� �ð��� ���� ȸ���� ���
            float timeSinceLastUpdate = Time.time - (nextAuraRotationTime - auraRotationInterval);
            float rotationAmount = rotationSpeed * timeSinceLastUpdate;

            // ����ȭ ����� �� ȸ���� ����
            if (isAuraOptimized)
            {
                // �÷��̾�Լ� �ָ� ���� ���� ���� ������ ����Ʈ�� ������Ʈ�ϵ�,
                // �ε巯�� ȸ���� ���� ������ ȸ������ �� ũ�� ����
                rotationAmount *= 1.5f;
            }

            // ���� ȸ�� ����
            auraTransform.Rotate(Vector3.forward * rotationAmount);

            // ���� ȸ�� �ð� ����
            nextAuraRotationTime = Time.time + auraRotationInterval;
        }
    }

    // �ø� ���� ���� �� �߰� �۾�
    public override void SetCullingState(bool isVisible)
    {
        base.SetCullingState(isVisible);

        // �ø� ���¿� ���� ���� Ȱ��ȭ/��Ȱ��ȭ
        if (auraTransform != null)
        {
            auraTransform.gameObject.SetActive(isVisible);
        }
    }

    // ����׿� �����
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (!Application.isPlaying) return;

        // ���� ����ȭ �Ÿ� ǥ��
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, auraOptimizationDistance);
    }
}