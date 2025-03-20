using UnityEngine;
using System.Collections;
using DG.Tweening;

public class EnemyDeathEffect : MonoBehaviour
{
    [Header("���� ����")]
    [SerializeField] private int particleCount = 5;
    [SerializeField] private float explosionDuration = 0.5f;
    [SerializeField] private float explosionRadius = 1f;
    [SerializeField] private Vector2 particleSizeRange = new Vector2(0.1f, 0.3f);

    [Header("��ƼŬ ����")]
    [SerializeField]
    private Color[] particleColors = new Color[]
    {
        new Color(1f, 0f, 0f),      // ����
        new Color(0f, 0f, 0f),      // ����
        new Color(65/255f, 65/255f, 65/255f)  // ȸ��
    };

    [Header("������Ʈ Ǯ ����")]
    [SerializeField] private string effectPoolTag = "DeathParticle"; // Ǯ �±׸��� GameManager�� ��ġ�ؾ� ��

    // ��ƼŬ ���� ����
    private static readonly int maxConcurrentEffects = 3; // ���ÿ� �߻� ������ �ִ� ȿ�� ��
    private static int activeEffectCount = 0;

    // ĳ���� ���� ����
    private static readonly WaitForSeconds particleDelay = new WaitForSeconds(0.02f);

    // �����ڿ��� ���� �ʵ� �ʱ�ȭ ���� (���� ����ȭ)
    static EnemyDeathEffect() { }

    // ���Ͱ� ���� �� ȣ��� �޼���
    public void PlayDeathEffect(Vector3 position)
    {
        // �ִ� ���� ȿ�� �� ���� Ȯ��
        if (activeEffectCount >= maxConcurrentEffects)
            return;

        // Ǯ ���� Ȯ�� - GameManager���� �̹� �ʱ�ȭ�����Ƿ� Ȯ�θ� ��
        if (ObjectPool.Instance == null || !ObjectPool.Instance.DoesPoolExist(effectPoolTag))
        {
            Debug.LogWarning($"DeathParticle pool not found. Skipping effect.");
            return;
        }

        // ȿ�� ����
        StartCoroutine(CreateDeathEffect(position));
    }

    private IEnumerator CreateDeathEffect(Vector3 position)
    {
        activeEffectCount++;

        for (int i = 0; i < particleCount; i++)
        {
            // ������Ʈ Ǯ���� ��ƼŬ ��������
            GameObject particle = ObjectPool.Instance.SpawnFromPool(effectPoolTag, position, Quaternion.identity);
            if (particle != null)
            {
                ConfigureAndAnimateParticle(particle, position);
            }

            // �ð����� �ΰ� ��ƼŬ ����
            yield return particleDelay;
        }

        // ��� ��ƼŬ�� �ִϸ��̼��� �Ϸ��ϱ� ���� ����� �ð� ���
        yield return new WaitForSeconds(explosionDuration);

        activeEffectCount--;
    }

    private void ConfigureAndAnimateParticle(GameObject particle, Vector3 position)
    {
        // ������ ��������
        SpriteRenderer renderer = particle.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            Debug.LogWarning("Particle is missing SpriteRenderer component");
            return;
        }

        // ���� ����
        float size = Random.Range(particleSizeRange.x, particleSizeRange.y);
        Color color = particleColors[Random.Range(0, particleColors.Length)];
        float angle = Random.Range(0f, 360f);
        float distance = explosionRadius * Random.Range(0.5f, 1f);

        // ������ ����
        renderer.color = color;

        // ��ǥ ��ġ ���
        Vector3 targetPos = position + new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
            Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
            0f
        );

        // ���� DOTween �ִϸ��̼� ����
        DOTween.Kill(particle.transform);
        DOTween.Kill(renderer);

        // DOTween �ִϸ��̼�
        Sequence seq = DOTween.Sequence();

        // �ʱ� ����
        particle.transform.localScale = Vector3.zero;
        renderer.color = new Color(color.r, color.g, color.b, 1f);

        // ũ�� ����
        seq.Append(particle.transform.DOScale(new Vector3(size, size, 1f), explosionDuration * 0.2f));

        // �̵�
        seq.Join(particle.transform.DOMove(targetPos, explosionDuration)
            .SetEase(Ease.OutQuad));

        // ȸ��
        seq.Join(particle.transform.DORotate(
            new Vector3(0f, 0f, Random.Range(-180f, 180f)),
            explosionDuration,
            RotateMode.FastBeyond360
        ).SetEase(Ease.OutQuad));

        // ���̵� �ƿ�
        seq.Join(renderer.DOFade(0f, explosionDuration)
            .SetEase(Ease.InQuad));

        // �Ϸ� �� ������Ʈ Ǯ�� ��ȯ
        seq.OnComplete(() => {
            if (ObjectPool.Instance != null)
            {
                ObjectPool.Instance.ReturnToPool(effectPoolTag, particle);
            }
        });

        // �ִϸ��̼��� �߰��� �ߴܵ� ��츦 ����� ������ġ
        seq.SetUpdate(true); // TimeScale�� ������� �ʵ��� ����
    }

    private void OnDestroy()
    {
        // ���� ���� ��� DOTween �ִϸ��̼� ����
        DOTween.Kill(transform);
    }
}