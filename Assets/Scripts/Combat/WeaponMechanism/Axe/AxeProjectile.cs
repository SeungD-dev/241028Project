using UnityEngine;


public class AxeProjectile : MonoBehaviour
{
    private float damage;
    private Vector2 direction;
    private Vector2 returnDirection;
    private float speed;
    private bool isReturning = false;
    private Vector2 startPosition;
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float returnSpeedMultiplier = 1f;

    public void Initialize(float damage, Vector2 direction, float speed)
    {
        this.damage = damage;
        this.direction = direction.normalized;
        this.speed = speed;
        startPosition = transform.position;
    }

    private void Update()
    {
        // ���� ȸ��
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        if (!isReturning)
        {
            // ���� �������� �̵�
            transform.position += (Vector3)(direction * speed * Time.deltaTime);

            // �ִ� �Ÿ� ���� üũ
            if (Vector2.Distance(startPosition, transform.position) >= maxDistance)
            {
                isReturning = true;
                // ���ư��� ������ ���� ���� ������ ���ݴ�� ����
                returnDirection = -direction;
            }
        }
        else
        {
            // ������ returnDirection���� ��� �̵�
            transform.position += (Vector3)(returnDirection * speed * returnSpeedMultiplier * Time.deltaTime);
        }
    }

    // OnBecameInvisible�� ������Ʈ�� ȭ�� ������ ���� �� ȣ���
    private void OnBecameInvisible()
    {
        // ���ư��� �߿��� �ı�
        if (isReturning)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}