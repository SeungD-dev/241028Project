using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class LoadingSceneController : MonoBehaviour
{
    [Header("UI ���")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private Image loadingIcon;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("�ε� �ִϸ��̼�")]
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private bool clockwise = true;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("�� �޽���")]
    [SerializeField]
    private List<string> tipMessages = new List<string> {
        "������ ����ϴ� ����ġ ������ ��� �������ϼ���!",
        "�� ����� ��Ư�� Ư���� ������ �ֽ��ϴ�. �پ��� ���� ������ �õ��غ�����.",
        "Buster�� �⺻���� ���ݷ��� ���� ����ü�� �߻��մϴ�.",
        "Machinegun�� ���� ���� �ӵ��� ���� ����ü�� �߻��մϴ�.",
        "BeamSaber�� �ֺ��� ��� ������ �������� �ִ� ȸ�� �����Դϴ�.",
        "Cutter�� ���� �����ϰ� �ٽ� ���ƿ��� ����ü�� �߻��մϴ�.",
        "Sawblade�� ���� �ε����� ƨ�ܳ����� ����ü�� �߻��մϴ�.",
        "Shotgun�� ���� ���� ����ü�� ��ä�� ���·� �߻��մϴ�.",
        "Grinder�� ���� �����Ͽ� ���������� �������� �ִ� ����ü�� �߻��մϴ�.",
        "ForceField�� �÷��̾� �ֺ��� �������� �ִ� �ʵ带 �����մϴ�.",
        "������ ������ �� ���� ���ΰ� ����ġ�� ����մϴ�.",
        "��ٿ��� ���ҽ�Ű�� �� ���� ������ �� �ֽ��ϴ�.",
        "�˹� ȿ���� ������ �о ������ �Ÿ��� ������ �� �ְ� �մϴ�.",
        "���� ������ ������ ���� ������ �����ݴϴ�.",
        "�̵� �ӵ��� ������ ���ϴ� �� �߿��մϴ�.",
        "����ġ ȹ�� ������ �ø��� �ָ��ִ� ����ġ�� ������ �� �ֽ��ϴ�.",
        "ü�� ȸ�� ȿ���� ���̸� ���� ���ɼ��� �����մϴ�."
    };

    private int currentTipIndex = -1;
    private Coroutine tipCycleCoroutine;
    private bool isLoading = true;

    private void Awake()
    {
        // UI �ʱ� ����
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
        }

        if (progressBar != null)
        {
            progressBar.value = 0f;
        }

        if (progressText != null)
        {
            progressText.text = "0%";
        }
    }

    private void Start()
    {
        StartCoroutine(FadeIn());
        StartCoroutine(DelayedLoading());
        StartCoroutine(CycleTips());
    }

    private IEnumerator DelayedLoading()
    {
        // ���� ������ �ε�� ������ ª�� ���
        yield return new WaitForSeconds(0.2f);

        // ���� �� ǥ��
        DisplayRandomTip();

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager �ν��Ͻ��� ã�� �� �����ϴ�!");
            yield break;
        }

        // �ε� ���� ��Ȳ �̺�Ʈ ���
        GameManager.Instance.OnLoadingCompleted += HandleLoadingCompleted;
        GameManager.Instance.OnLoadingCancelled += HandleLoadingCancelled;

        // �ε� ���μ��� ����
        GameManager.Instance.StartLoadingProcess();

        // �ε� ���� ��Ȳ ������Ʈ �ڷ�ƾ ����
        StartCoroutine(UpdateLoadingProgressUI());
    }

    private IEnumerator FadeIn()
    {
        if (fadeCanvasGroup == null) yield break;

        float elapsedTime = 0f;
        fadeCanvasGroup.alpha = 1f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = 1f - (elapsedTime / fadeInDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
    }

    private IEnumerator FadeOut()
    {
        if (fadeCanvasGroup == null) yield break;

        float elapsedTime = 0f;
        fadeCanvasGroup.alpha = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = elapsedTime / fadeOutDuration;
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    private void DisplayRandomTip()
    {
        if (tipText == null || tipMessages.Count == 0) return;

        int randomIndex = Random.Range(0, tipMessages.Count);
        if (randomIndex == currentTipIndex && tipMessages.Count > 1)
        {
            randomIndex = (randomIndex + 1) % tipMessages.Count;
        }

        currentTipIndex = randomIndex;
        tipText.text = tipMessages[currentTipIndex];
    }

    private IEnumerator CycleTips()
    {
        if (tipText == null) yield break;

        WaitForSeconds waitForTipChange = new WaitForSeconds(5f);

        while (isLoading)
        {
            yield return waitForTipChange;

            // �� �޽��� ��ü (���̵� ȿ�� ����)
            yield return StartCoroutine(FadeTipText());
        }
    }

    private IEnumerator FadeTipText()
    {
        if (tipText == null) yield break;

        // ���̵� �ƿ�
        float duration = 0.3f;
        float elapsedTime = 0f;
        Color startColor = tipText.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            tipText.color = Color.Lerp(startColor, endColor, elapsedTime / duration);
            yield return null;
        }

        // �ؽ�Ʈ ����
        DisplayRandomTip();

        // ���̵� ��
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            tipText.color = Color.Lerp(endColor, startColor, elapsedTime / duration);
            yield return null;
        }

        tipText.color = startColor;
    }

    private IEnumerator UpdateLoadingProgressUI()
    {
        if (progressBar == null || progressText == null) yield break;

        float previousProgress = 0f;

        while (isLoading)
        {
            float currentProgress = GameManager.Instance.LoadingProgress;

            // ���α׷����ٰ� ����ġ�� ���۽����� ������ �ʵ��� �ε巴�� ����
            float smoothProgress = Mathf.Lerp(previousProgress, currentProgress, Time.deltaTime * 5f);
            progressBar.value = smoothProgress;
            progressText.text = $"{Mathf.Round(smoothProgress * 100)}%";
            previousProgress = smoothProgress;

            // �ε� ������ ȸ��
            if (loadingIcon != null)
            {
                float rotationAmount = rotationSpeed * Time.deltaTime * (clockwise ? -1 : 1);
                loadingIcon.transform.Rotate(0, 0, rotationAmount);
            }

            yield return null;
        }
    }

    private void HandleLoadingCompleted()
    {
        StartCoroutine(CompleteLoading());
    }

    private void HandleLoadingCancelled()
    {
        isLoading = false;

        // �ε� ��� �� ���� �޴��� ���ư���
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.MainMenu);
        }

        // ���̵� �ƿ� �� ���� �޴� ������ ��ȯ
        StartCoroutine(ReturnToMainMenu());
    }

    private IEnumerator CompleteLoading()
    {
        isLoading = false;

        // ���α׷����� 100% ���·� ����
        if (progressBar != null)
        {
            progressBar.value = 1f;
        }

        if (progressText != null)
        {
            progressText.text = "100%";
        }

        // ��� ��� �� ���̵� �ƿ� ȿ�� ����
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(FadeOut());

        // ��� �ڷ�ƾ ����
        if (tipCycleCoroutine != null)
        {
            StopCoroutine(tipCycleCoroutine);
        }

        // �̺�Ʈ ���� ����
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLoadingCompleted -= HandleLoadingCompleted;
            GameManager.Instance.OnLoadingCancelled -= HandleLoadingCancelled;
        }
    }

    private IEnumerator ReturnToMainMenu()
    {
        yield return StartCoroutine(FadeOut());

        // ���� �޴� ������ ��ȯ
        int mainMenuSceneIndex;
        if (GameManager.Instance != null &&
            GameManager.Instance.gameScene.TryGetValue(GameState.MainMenu, out mainMenuSceneIndex))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneIndex);
        }
    }

    private void OnDestroy()
    {
        // �̺�Ʈ ���� ����
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLoadingCompleted -= HandleLoadingCompleted;
            GameManager.Instance.OnLoadingCancelled -= HandleLoadingCancelled;
        }
    }
}