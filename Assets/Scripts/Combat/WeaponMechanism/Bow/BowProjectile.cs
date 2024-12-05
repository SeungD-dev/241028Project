using UnityEngine;

public class BowProjectile : MonoBehaviour
{
    private float damage;
    private Vector2 direction;
    private float speed;
    [SerializeField] private float maxDistance = 20f;
    private Vector2 startPosition;
    [SerializeField] private float rotationOffset;

    public void Initialize(float damage, Vector2 direction, float speed)
    {
        this.damage = damage;
        this.direction = direction;
        this.speed = speed;
        startPosition = transform.position;

        // ȭ���� ���� ����
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
    }

    private void Update()
    {
        // ����ü �̵�
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // �ִ� �Ÿ� üũ
        if (Vector2.Distance(startPosition, transform.position) > maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // �� ü�� ����
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            // ���� �浹�ϸ� ȭ�� �ı�
            Destroy(gameObject);
        }
    }
}
