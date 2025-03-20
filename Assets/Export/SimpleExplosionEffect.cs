using UnityEngine;
using System.Collections;
using DG.Tweening;

public class SimpleExplosionEffect : MonoBehaviour
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
        new Color(1f, 0f, 0f),
        new Color(0f, 0f, 0f),
        new Color(65/255f, 65/255f, 65/255f)
    };

    private Transform particleContainer;
    private ObjectPool squarePool;

    private void Awake()
    {
        // ��ƼŬ�� ���� �� �����̳� ����
        particleContainer = new GameObject("ParticleContainer").transform;
        particleContainer.SetParent(transform);
        particleContainer.localPosition = Vector3.zero;

        // ������Ʈ Ǯ ����
        squarePool = new ObjectPool(CreateSquareParticle, particleCount * 2);
    }

    // ���Ͱ� ���� �� ȣ��
    public void PlayExplosion()
    {
        StartCoroutine(CreateExplosion(transform.position));
    }

    private IEnumerator CreateExplosion(Vector3 position)
    {
        for (int i = 0; i < particleCount; i++)
        {
            GameObject square = squarePool.GetObject();
            if (square != null)
            {
                // ��ƼŬ �ʱ�ȭ
                square.transform.position = position;
                square.transform.rotation = Quaternion.identity;
                square.transform.localScale = Vector3.one;
                square.SetActive(true);

                // ���� ����
                float size = Random.Range(particleSizeRange.x, particleSizeRange.y);
                Color color = particleColors[Random.Range(0, particleColors.Length)];
                float angle = Random.Range(0f, 360f);
                float distance = explosionRadius * Random.Range(0.5f, 1f);

                // �簢�� ������ ����
                SpriteRenderer renderer = square.GetComponent<SpriteRenderer>();
                renderer.color = color;

                // ��ǥ ��ġ ���
                Vector3 targetPos = position + new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    0f
                );

                // DOTween �ִϸ��̼�
                Sequence seq = DOTween.Sequence();

                // ũ�� ����
                seq.Append(square.transform.DOScale(new Vector3(size, size, 1f), explosionDuration * 0.2f));

                // �̵�
                seq.Join(square.transform.DOMove(targetPos, explosionDuration)
                    .SetEase(Ease.OutQuad));

                // ȸ��
                seq.Join(square.transform.DORotate(
                    new Vector3(0f, 0f, Random.Range(-180f, 180f)),
                    explosionDuration,
                    RotateMode.FastBeyond360
                ).SetEase(Ease.OutQuad));

                // ���̵� �ƿ�
                seq.Join(renderer.DOFade(0f, explosionDuration)
                    .SetEase(Ease.InQuad));

                // �Ϸ� �� ������Ʈ Ǯ�� ��ȯ
                seq.OnComplete(() => {
                    square.SetActive(false);
                    squarePool.ReturnObject(square);
                });
            }

            // �ణ�� �ð����� �ΰ� ��ƼŬ ����
            yield return new WaitForSeconds(0.02f);
        }
    }

    // �簢�� ��ƼŬ ����
    private GameObject CreateSquareParticle()
    {
        GameObject square = new GameObject("SquareParticle");
        square.transform.SetParent(particleContainer);

        // ��������Ʈ ������ �߰�
        SpriteRenderer renderer = square.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.sortingOrder = 10; // ���ͺ��� �տ� ǥ�õǵ���

        square.SetActive(false);
        return square;
    }

    // �簢�� ��������Ʈ ����
    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(32, 32);
        Color fillColor = Color.white;

        // �ؽ�ó ä���
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, fillColor);
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }

    // ������ ������Ʈ Ǯ ����
    private class ObjectPool
    {
        private GameObject[] pool;
        private bool[] isUsed;
        private System.Func<GameObject> createFunc;

        public ObjectPool(System.Func<GameObject> createFunc, int size)
        {
            this.createFunc = createFunc;
            pool = new GameObject[size];
            isUsed = new bool[size];

            // Ǯ �̸� ä���
            for (int i = 0; i < size; i++)
            {
                pool[i] = createFunc();
                isUsed[i] = false;
            }
        }

        public GameObject GetObject()
        {
            // ��� ������ ������Ʈ ã��
            for (int i = 0; i < pool.Length; i++)
            {
                if (!isUsed[i])
                {
                    isUsed[i] = true;
                    return pool[i];
                }
            }

            // Ǯ�� ���� �� ��� ���� ���� (������)
            // GameObject newObj = createFunc();
            // System.Array.Resize(ref pool, pool.Length + 1);
            // System.Array.Resize(ref isUsed, isUsed.Length + 1);
            // pool[pool.Length - 1] = newObj;
            // isUsed[isUsed.Length - 1] = true;
            // return newObj;

            // �Ǵ� null ��ȯ
            return null;
        }

        public void ReturnObject(GameObject obj)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i] == obj)
                {
                    isUsed[i] = false;
                    break;
                }
            }
        }
    }

    // �׽�Ʈ�� �޼���
    [ContextMenu("�׽�Ʈ ����")]
    public void TestExplosion()
    {
        PlayExplosion();
    }

    // (0, 0) ��ǥ�� ���� ����Ʈ ����
    public void PlayExplosionAtOrigin()
    {
        StartCoroutine(CreateExplosion(Vector3.zero));
    }

    private void OnDestroy()
    {
        DOTween.Kill(transform);
    }
}