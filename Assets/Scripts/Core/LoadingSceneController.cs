using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class LoadingSceneController : MonoBehaviour
{
    [Header("UI Referecenes")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Loading Animation")]
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private bool clockwise = true;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Tip Messages")]
    [SerializeField]
    private List<string> tipMessages = new List<string> {
        "Tip Messages0",
        "Tip Messages1",
        "Tip Messages2"
    };

    private int currentTipIndex = -1;
    private Coroutine tipCycleCoroutine;
    private bool isLoading = true;

    private void Awake()
    {
        // UI 초기 설정
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
        // 씬이 완전히 로드될 때까지 짧게 대기
        yield return new WaitForSeconds(0.2f);

        // 랜덤 팁 표시
        DisplayRandomTip();

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager Instance Cannot be found");
            yield break;
        }

        // 로딩 진행 상황 이벤트 등록
        GameManager.Instance.OnLoadingCompleted += HandleLoadingCompleted;
        GameManager.Instance.OnLoadingCancelled += HandleLoadingCancelled;

        // 로딩 프로세스 시작
        GameManager.Instance.StartLoadingProcess();

        // 로딩 진행 상황 업데이트 코루틴 시작
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
        tipText.text ="Tip : "+tipMessages[currentTipIndex];
    }

    private IEnumerator CycleTips()
    {
        if (tipText == null) yield break;

        WaitForSeconds waitForTipChange = new WaitForSeconds(5f);

        while (isLoading)
        {
            yield return waitForTipChange;

            // 팁 메시지 교체 (페이드 효과 적용)
            yield return StartCoroutine(FadeTipText());
        }
    }

    private IEnumerator FadeTipText()
    {
        if (tipText == null) yield break;

        // 페이드 아웃
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

        // 텍스트 변경
        DisplayRandomTip();

        // 페이드 인
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

            // 프로그레스바가 지나치게 갑작스럽게 변하지 않도록 부드럽게 보간
            float smoothProgress = Mathf.Lerp(previousProgress, currentProgress, Time.deltaTime * 5f);
            progressBar.value = smoothProgress;
            progressText.text = $"{Mathf.Round(smoothProgress * 100)}%";
            previousProgress = smoothProgress;

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

        // 로딩 취소 시 메인 메뉴로 돌아가기
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.MainMenu);
        }

        // 페이드 아웃 후 메인 메뉴 씬으로 전환
        StartCoroutine(ReturnToMainMenu());
    }

    private IEnumerator CompleteLoading()
    {
        isLoading = false;

        // 프로그레스바 100% 상태로 유지
        if (progressBar != null)
        {
            progressBar.value = 1f;
        }

        if (progressText != null)
        {
            progressText.text = "100%";
        }

        // 잠시 대기 후 페이드 아웃 효과 적용
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(FadeOut());

        // 모든 코루틴 정리
        if (tipCycleCoroutine != null)
        {
            StopCoroutine(tipCycleCoroutine);
        }

        // 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLoadingCompleted -= HandleLoadingCompleted;
            GameManager.Instance.OnLoadingCancelled -= HandleLoadingCancelled;
        }
    }

    private IEnumerator ReturnToMainMenu()
    {
        yield return StartCoroutine(FadeOut());

        // 메인 메뉴 씬으로 전환
        int mainMenuSceneIndex;
        if (GameManager.Instance != null &&
            GameManager.Instance.gameScene.TryGetValue(GameState.MainMenu, out mainMenuSceneIndex))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneIndex);
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLoadingCompleted -= HandleLoadingCompleted;
            GameManager.Instance.OnLoadingCancelled -= HandleLoadingCancelled;
        }
    }
}