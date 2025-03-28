using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TitleSceneEffects : MonoBehaviour
{
    [Header("Ÿ��Ʋ �̹��� ����")]
    [SerializeField] private RectTransform titleImage;
    [SerializeField] private float glitchInterval = 0.5f;
    [SerializeField] private float glitchDuration = 0.1f;
    [SerializeField] private float shakeStrength = 5f;
    [SerializeField] private int shakeVibrato = 10;
    [SerializeField] private float shakeRandomness = 90f;

    [Header("�۸�ġ ȿ�� ����")]
    [SerializeField] private float colorGlitchIntensity = 0.1f;
    [SerializeField] private float positionGlitchIntensity = 10f;
    [SerializeField] private bool useColorGlitch = true;
    [SerializeField] private bool usePositionGlitch = true;

    [Header("�����̵� �̹��� ����")]
    [SerializeField] private GameObject slidingImagePrefab; // �����̵� �̹��� ������
    [SerializeField] private Transform slidingImagesParent; // �����̵� �̹��� �θ� ������Ʈ
    [SerializeField] private bool isVerticalSlide = true; // true: ��/�Ʒ�, false: ��/��
    [SerializeField] private bool startFromTop = true; // true: ������ �Ʒ���, false: �Ʒ��� ����
    [SerializeField] private bool startFromLeft = true; // true: ���ʿ��� ����������, false: �����ʿ��� ��������
    [SerializeField] private float slideDuration = 3f; // �̵��� �ɸ��� �ð�
    [SerializeField] private float spawnInterval = 2f; // �̹��� ���� ����
    [SerializeField] private int poolSize = 10; // ������Ʈ Ǯ ũ��

    [Header("�̹��� ȿ�� ����")]
    [SerializeField] private float minScale = 0.5f; // �ּ� ũ��
    [SerializeField] private float maxScale = 1.5f; // �ִ� ũ��
    [SerializeField] private float blinkChance = 0.3f; // ������ Ȯ�� (0-1)
    [SerializeField] private float blinkInterval = 0.1f; // ������ ����
    [SerializeField] private int maxBlinkCount = 5; // �ִ� ������ Ƚ��

    // ������Ʈ ĳ��
    private Image titleImageComponent;
    private Color originalColor;
    private Vector2 originalPosition;

    // ������ ����� ����
    private Sequence titleSequence;
    private Coroutine glitchCoroutine;
    private Coroutine spawnCoroutine;

    // ������Ʈ Ǯ
    private List<RectTransform> slidingImagePool;
    private Queue<RectTransform> availableImages;
    private Canvas parentCanvas;

    void Awake()
    {
        // ĵ���� ���� ���
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            parentCanvas = GetComponent<Canvas>();
        }

        // ������Ʈ Ǯ �ʱ�ȭ
        InitializeObjectPool();
    }

    void Start()
    {
        // ������Ʈ �ʱ�ȭ
        if (titleImage != null)
        {
            titleImageComponent = titleImage.GetComponent<Image>();
            if (titleImageComponent != null)
            {
                originalColor = titleImageComponent.color;
                originalPosition = titleImage.anchoredPosition;
            }
        }

        // �ִϸ��̼� ����
        InitializeGlitchEffect();
        spawnCoroutine = StartCoroutine(SpawnSlidingImagesRoutine());
    }

    void OnDestroy()
    {
        // ������ ����
        titleSequence?.Kill();

        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
        }

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        // ���� ���·� ����
        if (titleImageComponent != null)
        {
            titleImageComponent.color = originalColor;
            titleImage.anchoredPosition = originalPosition;
        }

        // Ȱ��ȭ�� ��� �����̵� �̹��� ����
        foreach (var img in slidingImagePool)
        {
            if (img != null && img.gameObject.activeSelf)
            {
                DOTween.Kill(img);
                img.gameObject.SetActive(false);
            }
        }
    }

    private void InitializeObjectPool()
    {
        if (slidingImagePrefab == null || slidingImagesParent == null)
        {
            Debug.LogError("�����̵� �̹��� ������ �Ǵ� �θ� ������Ʈ�� �Ҵ���� �ʾҽ��ϴ�.");
            return;
        }

        slidingImagePool = new List<RectTransform>();
        availableImages = new Queue<RectTransform>();

        // Ǯ ����� �°� ��� ������Ʈ �̸� ����
        for (int i = 0; i < poolSize; i++)
        {
            GameObject newObj = Instantiate(slidingImagePrefab, slidingImagesParent);
            RectTransform rectTransform = newObj.GetComponent<RectTransform>();

            if (rectTransform == null)
            {
                Debug.LogError("�����̵� �̹��� �����տ� RectTransform ������Ʈ�� �����ϴ�.");
                continue;
            }

            // �̹��� ������Ʈ ĳ�� (������ ȿ����)
            Image imageComponent = newObj.GetComponent<Image>();
            if (imageComponent == null)
            {
                Debug.LogWarning("�����̵� �̹��� �����տ� Image ������Ʈ�� �����ϴ�. ������ ȿ���� ������� �ʽ��ϴ�.");
            }

            newObj.name = "SlidingImage_" + i;
            newObj.SetActive(false);
            slidingImagePool.Add(rectTransform);
            availableImages.Enqueue(rectTransform);
        }

        Debug.Log($"�����̵� �̹��� Ǯ �ʱ�ȭ �Ϸ�: {poolSize}�� ������");
    }

    private RectTransform GetSlidingImageFromPool()
    {
        if (availableImages.Count == 0)
        {
            // ��� ������Ʈ�� ��� ���̸� ���� ������ ���� ��Ȱ��
            RectTransform oldestImage = slidingImagePool[0];
            DOTween.Kill(oldestImage); // ���� �ִϸ��̼� ����
            oldestImage.gameObject.SetActive(false);
            return oldestImage;
        }

        RectTransform image = availableImages.Dequeue();
        image.gameObject.SetActive(true);
        return image;
    }

    private void ReturnImageToPool(RectTransform image)
    {
        image.gameObject.SetActive(false);
        availableImages.Enqueue(image);
    }

    private void InitializeGlitchEffect()
    {
        if (titleImage == null) return;

        // �۸�ġ �ڷ�ƾ ����
        glitchCoroutine = StartCoroutine(GlitchEffectRoutine());

        // ��鸲 ȿ���� ���� ������ ���� - �����ϰ� ��ġ ��鸲�� ����
        titleSequence = DOTween.Sequence();

        // �������� ���� ��鸲 ȿ��
        titleSequence.Append(
            titleImage.DOShakePosition(
                duration: 1.5f,
                strength: new Vector3(shakeStrength * 0.7f, shakeStrength * 0.3f, 0),
                vibrato: shakeVibrato,
                randomness: shakeRandomness,
                snapping: false,
                fadeOut: true
            )
        ).SetLoops(-1, LoopType.Restart);
    }

    private IEnumerator GlitchEffectRoutine()
    {
        WaitForSeconds glitchWait = new WaitForSeconds(glitchDuration);
        WaitForSeconds intervalWait = new WaitForSeconds(glitchInterval - glitchDuration);

        while (true)
        {
            // �۸�ġ ȿ�� ����
            ApplyGlitchEffect(true);
            yield return glitchWait;

            // ���� ���·� ����
            ApplyGlitchEffect(false);
            yield return intervalWait;
        }
    }

    private void ApplyGlitchEffect(bool apply)
    {
        if (titleImageComponent == null) return;

        if (apply)
        {
            // 1. ���� �۸�ġ
            if (useColorGlitch)
            {
                Color glitchColor = new Color(
                    originalColor.r + Random.Range(-colorGlitchIntensity, colorGlitchIntensity),
                    originalColor.g + Random.Range(-colorGlitchIntensity, colorGlitchIntensity),
                    originalColor.b + Random.Range(-colorGlitchIntensity, colorGlitchIntensity),
                    originalColor.a
                );
                titleImageComponent.color = glitchColor;
            }

            // 2. ��ġ �۸�ġ - DOTween ��鸲�� ������ �߰� ȿ��
            if (usePositionGlitch)
            {
                Vector2 glitchPosition = new Vector2(
                    originalPosition.x + Random.Range(-positionGlitchIntensity, positionGlitchIntensity),
                    originalPosition.y + Random.Range(-positionGlitchIntensity, positionGlitchIntensity)
                );
                titleImage.anchoredPosition = glitchPosition;
            }
        }
        else
        {
            // ���� ���·� ���� ���� (��ġ�� DOTween���� ó��)
            titleImageComponent.color = originalColor;

            // ��ġ �۸�ġ�� �������� ���, ��ġ�� ����
            if (usePositionGlitch)
            {
                titleImage.anchoredPosition = originalPosition;
            }
        }
    }

    private IEnumerator SpawnSlidingImagesRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(spawnInterval);

        while (true)
        {
            SpawnSlidingImage();
            yield return wait;
        }
    }

    private void SpawnSlidingImage()
    {
        if (slidingImagePool.Count == 0 || availableImages.Count == 0 || parentCanvas == null) return;

        RectTransform imageRect = GetSlidingImageFromPool();
        Image imageComponent = imageRect.GetComponent<Image>();

        // ȭ�� ũ�� ��� (ĵ������ RectTransform ���)
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        // ���� ������ ����
        float randomScale = Random.Range(minScale, maxScale);
        imageRect.localScale = new Vector3(randomScale, randomScale, 1f);

        // ���� �� �� ��ġ ���
        Vector2 startPos, endPos;

        // X ��ġ�� �ణ�� ������ �߰�
        float randomXOffset = Random.Range(-canvasWidth * 0.3f, canvasWidth * 0.3f);
        // Y ��ġ�� �ణ�� ������ �߰�
        float randomYOffset = Random.Range(-canvasHeight * 0.3f, canvasHeight * 0.3f);

        if (isVerticalSlide)
        {
            // ���� �̵� (������ �Ʒ� �Ǵ� �Ʒ��� ��)
            if (startFromTop)
            {
                // ������ ����
                startPos = new Vector2(randomXOffset, canvasHeight / 2 + 100);
                endPos = new Vector2(randomXOffset, -canvasHeight / 2 - 100);
            }
            else
            {
                // �Ʒ��� ����
                startPos = new Vector2(randomXOffset, -canvasHeight / 2 - 100);
                endPos = new Vector2(randomXOffset, canvasHeight / 2 + 100);
            }
        }
        else
        {
            // ���� �̵� (���ʿ��� ������ �Ǵ� �����ʿ��� ����)
            if (startFromLeft)
            {
                // ���ʿ��� ����
                startPos = new Vector2(-canvasWidth / 2 - 100, randomYOffset);
                endPos = new Vector2(canvasWidth / 2 + 100, randomYOffset);
            }
            else
            {
                // �����ʿ��� ����
                startPos = new Vector2(canvasWidth / 2 + 100, randomYOffset);
                endPos = new Vector2(-canvasWidth / 2 - 100, randomYOffset);
            }
        }

        // ���� ��ġ ����
        imageRect.anchoredPosition = startPos;

        // �̵� ������ ����
        Sequence slideSequence = DOTween.Sequence();

        // ���� �̵� (sway ����)
        slideSequence.Append(
            imageRect.DOAnchorPos(endPos, slideDuration).SetEase(Ease.Linear)
        );

        // ������ ȿ�� �߰� (�����ϰ�) - ��� ���İ� ���� ������� ����
        if (imageComponent != null && Random.value < blinkChance)
        {
            // �� �� �������� �����ϰ� ����
            int blinkCount = Random.Range(2, maxBlinkCount);

            for (int i = 0; i < blinkCount; i++)
            {
                // ������ �ð��� ������ �߻�
                float blinkTime = Random.Range(slideDuration * 0.1f, slideDuration * 0.9f);

                // ���������� ���İ� 0���� ���� (Ease.INTERNAL_Zero = ��� ����)
                slideSequence.InsertCallback(blinkTime, () => {
                    Color tempColor = imageComponent.color;
                    tempColor.a = 0f;
                    imageComponent.color = tempColor;
                });

                // ���������� ���İ� 1�� ���� (��� ����)
                slideSequence.InsertCallback(blinkTime + blinkInterval, () => {
                    Color tempColor = imageComponent.color;
                    tempColor.a = 1f;
                    imageComponent.color = tempColor;
                });
            }
        }

        // �Ϸ� �� ������Ʈ Ǯ�� ��ȯ
        slideSequence.OnComplete(() => ReturnImageToPool(imageRect));
    }

    // �ν����Ϳ��� ȿ�� ����� ��ư��
    public void RestartEffects()
    {
        // ���� ȿ�� ����
        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
        }

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        titleSequence?.Kill();

        // ���� ���·� ����
        if (titleImageComponent != null)
        {
            titleImageComponent.color = originalColor;
            titleImage.anchoredPosition = originalPosition;
        }

        // Ȱ��ȭ�� ��� �����̵� �̹��� ����
        foreach (var img in slidingImagePool)
        {
            if (img != null && img.gameObject.activeSelf)
            {
                DOTween.Kill(img);
                img.gameObject.SetActive(false);
                availableImages.Enqueue(img);
            }
        }

        // ȿ�� �ٽ� ����
        glitchCoroutine = StartCoroutine(GlitchEffectRoutine());
        InitializeGlitchEffect();
        spawnCoroutine = StartCoroutine(SpawnSlidingImagesRoutine());
    }

    // �ܺο��� ȿ�� ������ ������ �� �ִ� �޼����
    public void SetGlitchIntensity(float colorIntensity, float positionIntensity)
    {
        colorGlitchIntensity = colorIntensity;
        positionGlitchIntensity = positionIntensity;
    }

    public void SetGlitchInterval(float interval, float duration)
    {
        glitchInterval = Mathf.Max(0.1f, interval);
        glitchDuration = Mathf.Min(duration, glitchInterval);

        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
            glitchCoroutine = StartCoroutine(GlitchEffectRoutine());
        }
    }

    public void SetSlideDirection(bool vertical, bool fromTopOrLeft)
    {
        isVerticalSlide = vertical;
        if (vertical)
        {
            startFromTop = fromTopOrLeft;
        }
        else
        {
            startFromLeft = fromTopOrLeft;
        }
    }

    public void SetSpawnRate(float interval)
    {
        spawnInterval = interval;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = StartCoroutine(SpawnSlidingImagesRoutine());
        }
    }

    public void SetScaleRange(float min, float max)
    {
        minScale = Mathf.Max(0.1f, min);
        maxScale = Mathf.Max(minScale, max);
    }

    public void SetBlinkEffect(float chance, float interval)
    {
        blinkChance = Mathf.Clamp01(chance);
        blinkInterval = Mathf.Max(0.01f, interval);
    }
}
