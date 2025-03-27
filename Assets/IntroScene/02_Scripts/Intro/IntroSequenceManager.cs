using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.SceneManagement;

/// <summary>
/// ��Ʈ�� ������ ���� �� ������ ����ϴ� Ŭ����
/// ����ȭ�� �������� GameManager�� ���յ�
/// </summary>
public class IntroSequenceManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image blackOverlay;           // ���̵� ��/�ƿ��� ������ �̹���
    [SerializeField] private RectTransform scrollImage;    // ��ũ�ѵ� ���� �̹���
    [SerializeField] private TextMeshProUGUI introText;    // �ؽ�Ʈ ǥ�ÿ� UI

    [Header("Panel Fade Settings")]
    [SerializeField] private float stepDuration = 0.3f;    // �� �ܰ� ������ �ð� ����
    [SerializeField] private int fadeSteps = 4;            // ���İ� �ܰ� ��
    [SerializeField] private float initialPanelAlpha = 1.0f;  // ���� �� �г� ���İ�
    [SerializeField] private float finalPanelAlpha = 0.0f;    // ���� ȭ�鿡���� �г� ���İ�

    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 50f;      // �ʴ� ��ũ�� �ȼ�
    [SerializeField] private float initialDelay = 0.5f;    // ���̵� �� �� ��ũ�� ���� �� ��� �ð�
    [SerializeField] private float scrollEndY = 2000f;     // ��ũ���� ������ Y ��ġ
    [SerializeField] private float intervalBetweenTexts = 0.5f;  // �ؽ�Ʈ ���� ����

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

    [SerializeField] private List<IntroTextItem> introTextSequence = new List<IntroTextItem>();

    // �̹��� ��ġ ���� ����
    private float scrollY = 0f;
    private bool isScrolling = false;
    private bool sequenceCompleted = false;
    private bool isTransitioning = false;

    // �ڷ�ƾ ���� ����
    private Coroutine introSequenceCoroutine;

    // ĳ�õ� WaitForSeconds ��ü
    private WaitForSeconds initialDelayWait;
    private WaitForSeconds intervalWait;
    private WaitForSeconds stepDelayWait;
    private Dictionary<float, WaitForSeconds> typingDelays = new Dictionary<float, WaitForSeconds>();
    private Dictionary<float, WaitForSeconds> displayTimeWaits = new Dictionary<float, WaitForSeconds>();

    private void Awake()
    {
        // ���� ����ȭ�� ���� ���� ���Ǵ� WaitForSeconds ��ü ĳ��
        initialDelayWait = new WaitForSeconds(initialDelay);
        intervalWait = new WaitForSeconds(intervalBetweenTexts);
        stepDelayWait = new WaitForSeconds(stepDuration);

        // Ÿ�Զ����� ȿ���� ǥ�� �ð��� ���� WaitForSeconds ĳ��
        CacheWaitForSecondsObjects();
    }

    private void CacheWaitForSecondsObjects()
    {
        // �ؽ�Ʈ ǥ�� �ð� ĳ��
        HashSet<float> displayTimes = new HashSet<float>();
        HashSet<float> typingSpeeds = new HashSet<float>();

        foreach (var item in introTextSequence)
        {
            displayTimes.Add(item.displayTime);
            if (item.useTypewriterEffect)
            {
                typingSpeeds.Add(item.typingSpeed);
            }
        }

        // ������ ǥ�� �ð��� ���� WaitForSeconds ��ü ����
        foreach (float time in displayTimes)
        {
            if (!displayTimeWaits.ContainsKey(time))
            {
                displayTimeWaits[time] = new WaitForSeconds(time);
            }
        }

        // ������ Ÿ���� �ӵ��� ���� WaitForSeconds ��ü ����
        foreach (float speed in typingSpeeds)
        {
            if (!typingDelays.ContainsKey(speed))
            {
                typingDelays[speed] = new WaitForSeconds(speed);
            }
        }
    }

    private void Start()
    {
        PrepareUI();

        SetupSounds();


        // ��Ʈ�� ������ ����
        introSequenceCoroutine = StartCoroutine(PlayIntroSequence());
    }
    private void SetupSounds()
    {
        if (SoundManager.Instance != null)
        {
            // ���� ��� ���� BGM Ȯ��
            bool isBgmPlaying = SoundManager.Instance.IsBGMPlaying("BGM_Intro");

            // ��Ʈ�� �����ũ �ε� (���� �ε���� �ʾҴٸ�)
            if (SoundManager.Instance.currentSoundBank == null ||
                SoundManager.Instance.currentSoundBank.name != "IntroSoundBank")
            {
                SoundManager.Instance.LoadSoundBank("IntroSoundBank");
            }

            // �̹� ��� ���� ��찡 �ƴ϶�� BGM ���
            if (!isBgmPlaying)
            {
                SoundManager.Instance.PlaySound("BGM_Intro", 1f, true);
            }
        }
        else
        {
            Debug.LogWarning("SoundManager not found!");
        }
    }
    private void PrepareUI()
    {
        // �ʱ� ����
        if (introText != null)
            introText.alpha = 0f;

        // �ʱ� �г� ����
        if (blackOverlay != null)
            blackOverlay.color = new Color(0, 0, 0, initialPanelAlpha);

        // �ʱ� ��ũ�� ��ġ ����
        scrollY = 0f;
        if (scrollImage != null)
            scrollImage.anchoredPosition = new Vector2(scrollImage.anchoredPosition.x, scrollY);
    }

    private IEnumerator PlayIntroSequence()
    {
        // 1. ���� �� �г��� �ܰ������� �����
        yield return StepFadePanel(initialPanelAlpha, finalPanelAlpha, fadeSteps, stepDuration);

        // 2. �ʱ� ������
        yield return initialDelayWait;

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

        // 7. ��Ʈ�� �Ϸ� �� Ÿ��Ʋ������ �̵�
        CompleteIntro();
    }

    private IEnumerator StepFadePanel(float startAlpha, float targetAlpha, int steps, float stepDelay)
    {
        if (blackOverlay == null) yield break;

        // ���۰��� ��ǥ�� ������ ���� ���
        float alphaStep = (targetAlpha - startAlpha) / steps;
        Color color = blackOverlay.color;

        for (int i = 0; i <= steps; i++)
        {
            // ���� �ܰ迡 �´� ���İ� ���
            color.a = startAlpha + (alphaStep * i);
            blackOverlay.color = color;

            // ���� �ܰ� �� ���
            yield return stepDelayWait;
        }
    }

    private IEnumerator ShowTextSequence()
    {
        if (introText == null) yield break;

        Color textColor = introText.color;
        Color panelColor = blackOverlay != null ? blackOverlay.color : Color.black;

        for (int i = 0; i < introTextSequence.Count; i++)
        {
            IntroTextItem textItem = introTextSequence[i];

            // �г� ǥ�� (�ؽ�Ʈ�� �Բ�)
            if (textItem.showPanelWithText && blackOverlay != null)
            {
                panelColor.a = textItem.panelAlpha;
                blackOverlay.color = panelColor;
            }

            if (textItem.useTypewriterEffect)
            {
                // �ؽ�Ʈ �ʱ�ȭ
                introText.text = "";
                textColor.a = 1f;
                introText.color = textColor;

                // Ÿ���� ȿ��
                yield return TypeText(textItem.text, textItem.typingSpeed);

                // ǥ�� �ð� ��� (ĳ�õ� WaitForSeconds ���)
                yield return GetDisplayTimeWait(textItem.displayTime);
            }
            else
            {
                // �ؽ�Ʈ ����
                introText.text = textItem.text;
                textColor.a = 1f;
                introText.color = textColor;

                // ǥ�� �ð� ��� (ĳ�õ� WaitForSeconds ���)
                yield return GetDisplayTimeWait(textItem.displayTime);
            }

            // �ؽ�Ʈ ����
            textColor.a = 0f;
            introText.color = textColor;

            // �г� ���İ� �ǵ�����
            if (textItem.showPanelWithText && blackOverlay != null)
            {
                panelColor.a = finalPanelAlpha;
                blackOverlay.color = panelColor;
            }

            // ��� �ؽ�Ʈ�� ������ ���� ����
            yield return intervalWait;
        }
    }

    // �ؽ�Ʈ�� Ÿ���εǴ� ��ó�� �� ���ھ� ����ϴ� �Լ�
    private IEnumerator TypeText(string fullText, float typingSpeed)
    {
        WaitForSeconds typeDelay = GetTypingSpeedWait(typingSpeed);

        introText.text = "";
        for (int i = 0; i <= fullText.Length; i++)
        {
            introText.text = fullText.Substring(0, i);
            yield return typeDelay;
        }
    }

    private WaitForSeconds GetTypingSpeedWait(float speed)
    {
        // ĳ�õ� WaitForSeconds ��ü ��ȯ
        if (typingDelays.TryGetValue(speed, out WaitForSeconds wait))
        {
            return wait;
        }

        // ������ ���� �����ϰ� ĳ��
        wait = new WaitForSeconds(speed);
        typingDelays[speed] = wait;
        return wait;
    }

    private WaitForSeconds GetDisplayTimeWait(float time)
    {
        // ĳ�õ� WaitForSeconds ��ü ��ȯ
        if (displayTimeWaits.TryGetValue(time, out WaitForSeconds wait))
        {
            return wait;
        }

        // ������ ���� �����ϰ� ĳ��
        wait = new WaitForSeconds(time);
        displayTimeWaits[time] = wait;
        return wait;
    }

    private void Update()
    {
        if (isScrolling && scrollImage != null)
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

    /// <summary>
    /// ��Ʈ�θ� �Ϸ��ϰ� ���� ������ �̵�
    /// </summary>
    private void CompleteIntro()
    {
        // �̹� ��ȯ ���� ��� �ߺ� ���� ����
        if (isTransitioning) return;
        isTransitioning = true;

        // BGM ���̵� �ƿ� (CrossfadeBGM�� ����)
        if (SoundManager.Instance != null && SoundManager.Instance.IsBGMPlaying("BGM_Intro"))
        {
            // ���� ������ ��ȯ �� ����� ���̵� �ƿ�
            // SoundManager�� CrossfadeBGM�� ���� �޼����̹Ƿ� ���� ȣ������ �ʰ�
            // �ٸ� �� ����� ���̵��ϰų� ���� ������ ���� ó��
            SoundManager.Instance.SetBGMVolume(0f); // ������ 0���� �����Ͽ� ���̵� �ƿ� ȿ��
        }

        // GameManager�� ���� Ÿ��Ʋ������ �̵�
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteIntro();
        }
        else
        {
            // GameManager�� ���� ��� ���� Ÿ��Ʋ������ �̵�
            SceneManager.LoadScene(1); // TitleScene �ε���
        }
    }

    /// <summary>
    /// ��Ʈ�θ� �ǳʶٰ� Ÿ��Ʋ������ ��� �̵�
    /// </summary>
    public void SkipIntro()
    {
        Debug.Log("IntroSequenceManager.SkipIntro() ����");

        try
        {
            if (isTransitioning)
            {
                Debug.Log("�̹� ��ȯ ���̹Ƿ� SkipIntro ���õ�");
                return;
            }

            isTransitioning = true;
            Debug.Log("isTransitioning = true�� ������");

            // ȿ���� ���
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySound("Button_sfx", 0.5f, false);
                Debug.Log("ȿ���� �����");
            }

            // ���� ���� �ڷ�ƾ �ߴ�
            Debug.Log("��� �ڷ�ƾ �ߴ�");
            StopAllCoroutines();

            // ��� ���� ȭ������ ��ȯ
            if (blackOverlay != null)
            {
                Debug.Log("���� ȭ������ ��ȯ");
                Color color = blackOverlay.color;
                color.a = initialPanelAlpha;
                blackOverlay.color = color;
            }

            // ���� ������ ��ȯ - �����ϰ� �ڷ�ƾ���� �и�
            Debug.Log("DirectCompleteIntro �ڷ�ƾ ����");
            StartCoroutine(DirectCompleteIntro());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SkipIntro���� ���� �߻�: {e.Message}\n{e.StackTrace}");

            // ������ �õ��� ���� �� �ε�
            try
            {
                Debug.Log("���� �߻� �� ���� Ÿ��Ʋ�� �ε� �õ�");
                SceneManager.LoadScene(1);
            }
            catch (System.Exception e2)
            {
                Debug.LogError($"���� �� �ε� �õ� �� ���� �߻�: {e2.Message}");
            }
        }
    }

    private IEnumerator DirectCompleteIntro()
    {
        yield return null; // �����ϰ� 1������ ���

        Debug.Log("DirectCompleteIntro ���� ��");

        // ��Ʈ�� BGM ���̵� �ƿ� (0.5�� ����)
        if (SoundManager.Instance != null && SoundManager.Instance.IsBGMPlaying("BGM_Intro"))
        {
            Debug.Log("��Ʈ�� BGM ���̵� �ƿ�");
            SoundManager.Instance.FadeOutBGM(0.5f);
        }

        // ���̵� �ƿ��� �Ϸ�� ������ �ణ ��� (���û���)
        yield return new WaitForSecondsRealtime(0.2f);  // ���̵� �ƿ��� �Ϻθ� ��ٸ��� �� ��ȯ

        try
        {
            // GameManager�� ���� �� ��ȯ���� ����
            if (GameManager.Instance != null)
            {
                Debug.Log("GameManager.CompleteIntro() ȣ��");
                GameManager.Instance.CompleteIntro();
            }
            else
            {
                // GameManager�� ���� ��쿡�� ���� �� ��ȯ
                Debug.Log("GameManager ����, ���� Ÿ��Ʋ������ ��ȯ");
                SceneManager.LoadScene(1);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"�� ��ȯ �� ���� �߻�: {e.Message}\n{e.StackTrace}");

            // ������ �õ��� ���� �� �ε�
            try
            {
                SceneManager.LoadScene(1);
            }
            catch (System.Exception e2)
            {
                Debug.LogError($"���� �� �ε� �õ� �� ���� �߻�: {e2.Message}");
            }
        }
    }

    private void OnDestroy()
    {
        // ���ҽ� ����
        StopAllCoroutines();
        DOTween.Kill(transform);

        // ��ųʸ� ����
        typingDelays.Clear();
        displayTimeWaits.Clear();
    }
}