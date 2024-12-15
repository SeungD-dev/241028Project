using UnityEngine;

public class CutterProjectile : BaseProjectile
{
    private bool isReturning = false;
    [SerializeField] private float rotationSpeed = 720f;

    public override void OnObjectSpawn()
    {
        base.OnObjectSpawn();
        isReturning = false;  // ������ ������ ���� ����
    }

    protected override void Update()
    {
        if (startPosition == null || startPosition == Vector2.zero)
        {
            startPosition = transform.position;  // ���� ��ġ�� ������ ���� ��ġ�� ����
        }

        // �������� ȸ��
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        float distanceFromStart = Vector2.Distance(startPosition, transform.position);
        float distanceRatio = distanceFromStart / maxTravelDistance;
        if (!isReturning)
        {
            // ���� �������� �̵��ϸ鼭 �ӵ� ����
            float speedMultiplier = Mathf.Lerp(1f, 0.2f, distanceRatio);
            transform.Translate(direction * speed * speedMultiplier * Time.deltaTime, Space.World);

            // �ִ� �Ÿ� ���� �� ��ȯ ����
            if (distanceRatio >= 1f)
            {
                isReturning = true;
            }
        }
        else
        {
            // ���ƿ��� �������� �̵��ϸ鼭 �ӵ� ����
            float returnRatio = 1f - (distanceFromStart / maxTravelDistance);
            float speedMultiplier = Mathf.Lerp(0.5f, 2f, returnRatio);
            transform.Translate(-direction * speed * speedMultiplier * Time.deltaTime, Space.World);

            // ���� ���� ��ó�� �����ϸ� ����
            if (distanceFromStart <= 0.5f)
            {
                ReturnToPool();
            }
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        isReturning = false;  // ��Ȱ��ȭ�� ���� ���� ����
    }
}