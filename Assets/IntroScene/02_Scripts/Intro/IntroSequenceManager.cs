using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class IntroSequenceManager : MonoBehaviour
{
    [Header("UI References")]
    public Image blackOverlay;           // ���̵� ��/�ƿ��� ������ �̹���
    public RectTransform scrollImage;    // ��ũ�ѵ� ���� �̹���
    public TextMeshProUGUI introText;    // �ؽ�Ʈ ǥ�ÿ� UI

    [Header("Scene Transition")]
    public string nextSceneName;         // ��Ʈ�� ���� �� ��ȯ�� �� �̸�
    public bool loadNextSceneWhenDone = true; // ��Ʈ�� ���� �� �� ��ȯ ����

    [Header("Panel Fade Settings")]
    public float stepDuration = 0.3f;    // �� �ܰ� ������ �ð� ����
    public int fadeSteps = 4;            // ���İ� �ܰ� �� (�⺻ 4�ܰ�: 100%, 75%, 50%, 25%, 0%)
    public float initialPanelAlpha = 1.0f;  // ���� �� �г� ���İ�
    public float finalPanelAlpha = 0.0f;    // ���� ȭ�鿡���� �г� ���İ�

    [Header("Scroll Settings")]
    public float scrollSpeed = 50f;      // �ʴ� ��ũ�� �ȼ� (���� Ŭ���� ����)
    public float initialDelay = 0.5f;    // ���̵� �� �� ��ũ�� ���� �� ��� �ð�
    public float scrollEndY = 2000f;     // ��ũ���� ������ Y ��ġ (����� ����)
    public float intervalBetweenTexts = 0.5f;  // �ؽ�Ʈ ���� ����

    [System.Serializable]
    public class IntroTextItem
    {
        public string text;
        public float displayTime = 3.0f;  // �ؽ�Ʈ�� ȭ�鿡 ǥ�õǴ� �ð�
        public bool useTypewriterEffect = true;
        public float typingSpeed = 0.05f;  // Ÿ���� �ӵ� (���ڴ� ��)

        [Header("Panel Settings")]
        public bool showPanelWithText = true;  // �ؽ�Ʈ ǥ�� �� �г� ǥ�� ����
        public float panelAlpha = 0.5f;        // �ؽ�Ʈ ǥ�� �� �г� ���İ� (0-1)
    }
    public List<IntroTextItem> introTextSequence = new List<IntroTextItem>();

    // �̹��� ��ġ ���� ����
    private float scrollY = 0f;
    private bool isScrolling = false;
    private bool sequenceCompleted = false;

    void Start()
    {
        // �ʱ� ����
        if (introText != null)
            introText.alpha = 0f;

        // �ʱ� �г� ����
        blackOverlay.color = new Color(0, 0, 0, initialPanelAlpha);

        // �ʱ� ��ũ�� ��ġ ����
        scrollY = 0f;
        scrollImage.anchoredPosition = new Vector2(scrollImage.anchoredPosition.x, scrollY);

        // ��Ʈ�� ������ ����
        StartCoroutine(PlayIntroSequence());
    }

    IEnumerator PlayIntroSequence()
    {
        // 1. ���� �� �г��� �ܰ������� �����
        yield return StepFadePanel(initialPanelAlpha, finalPanelAlpha, fadeSteps, stepDuration);

        // 2. �ʱ� ������
        yield return new WaitForSeconds(initialDelay);

        // 3. ��ũ�� ����
        isScrolling = true;

        // 4. �ؽ�Ʈ ������ ����
        yield return ShowTextSequence();

        // 5. ��ũ�� ���Ḧ ��ٸ� (Update �Լ����� ó��)
        while (!sequenceCompleted)
        {
            yield return null;
        }

        // 6. ���� �� �г��� �ܰ������� ��Ÿ��
        yield return StepFadePanel(finalPanelAlpha, initialPanelAlpha, fadeSteps, stepDuration);

        // 7. �� ��ȯ
        if (loadNextSceneWhenDone && !string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    IEnumerator StepFadePanel(float startAlpha, float targetAlpha, int steps, float stepDelay)
    {
        // ���۰��� ��ǥ�� ������ ���� ���
        float alphaStep = (targetAlpha - startAlpha) / steps;

        for (int i = 0; i <= steps; i++)
        {
            // ���� �ܰ迡 �´� ���İ� ���
            float currentAlpha = startAlpha + (alphaStep * i);

            // ���İ��� ��� ����
            Color color = blackOverlay.color;
            color.a = currentAlpha;
            blackOverlay.color = color;

            // ���� �ܰ� �� ���
            yield return new WaitForSeconds(stepDelay);
        }
    }

    IEnumerator ShowTextSequence()
    {
        foreach (IntroTextItem textItem in introTextSequence)
        {
            // �г� ��� ǥ�� (�ؽ�Ʈ�� �Բ�)
            if (textItem.showPanelWithText)
            {
                // �г� ���İ� ��� ����
                Color panelColor = blackOverlay.color;
                panelColor.a = textItem.panelAlpha;
                blackOverlay.color = panelColor;
            }

            if (textItem.useTypewriterEffect)
            {
                // �ؽ�Ʈ �ʱ�ȭ
                introText.text = "";

                // �ؽ�Ʈ ��� ���̰� ����
                Color textColor = introText.color;
                textColor.a = 1f;
                introText.color = textColor;

                // Ÿ���� ȿ��
                yield return TypeText(textItem.text, textItem.typingSpeed);

                // ǥ�� �ð� ���
                yield return new WaitForSeconds(textItem.displayTime);
            }
            else
            {
                // �ؽ�Ʈ ����
                introText.text = textItem.text;

                // �ؽ�Ʈ ��� ���̰� ����
                Color textColor = introText.color;
                textColor.a = 1f;
                introText.color = textColor;

                // ǥ�� �ð� ���
                yield return new WaitForSeconds(textItem.displayTime);
            }

            // �ؽ�Ʈ ��� ����
            Color hideTextColor = introText.color;
            hideTextColor.a = 0f;
            introText.color = hideTextColor;

            // �г� ���İ� �ǵ�����
            if (textItem.showPanelWithText)
            {
                // �г� ���İ� ��� ����
                Color panelColor = blackOverlay.color;
                panelColor.a = finalPanelAlpha;
                blackOverlay.color = panelColor;
            }

            // ��� �ؽ�Ʈ�� ������ ���� ����
            yield return new WaitForSeconds(intervalBetweenTexts);
        }
    }

    // �ؽ�Ʈ�� Ÿ���εǴ� ��ó�� �� ���ھ� ����ϴ� �Լ�
    IEnumerator TypeText(string fullText, float typingSpeed)
    {
        introText.text = "";

        for (int i = 0; i <= fullText.Length; i++)
        {
            introText.text = fullText.Substring(0, i);
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    void Update()
    {
        if (isScrolling)
        {
            // ���� �ӵ��� �������� ��ũ�� (Y �� ����)
            scrollY += scrollSpeed * Time.deltaTime;
            scrollImage.anchoredPosition = new Vector2(scrollImage.anchoredPosition.x, scrollY);

            // ��ũ�� ���� ���� (������ �����ϸ�)
            if (scrollY > scrollEndY)
            {
                isScrolling = false;
                sequenceCompleted = true;
            }
        }
    }

    // �ν����Ϳ��� �׽�Ʈ�ϱ� ���� �޼ҵ�
    public void SkipIntro()
    {
        StopAllCoroutines();
        DOTween.KillAll();
        isScrolling = false;
        sequenceCompleted = true;

        // ��� ���� ȭ������
        Color color = blackOverlay.color;
        color.a = initialPanelAlpha;
        blackOverlay.color = color;

        // �� ��ȯ
        if (loadNextSceneWhenDone && !string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}