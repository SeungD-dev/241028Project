using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private Button optionButton;
    [SerializeField] private TextBlinkEffect touchToEngageText;

    [Header("Touch Detection")]
    [SerializeField] private float touchDelay = 0.5f; // �Ǽ��� ��ġ�ϴ� ���� �����ϱ� ���� ���� �ð�

    private TouchActions touchActions;
    private bool canStartGame = false;
    private bool isTransitioning = false;

    private void Awake()
    {
        // Input Actions �ʱ�ȭ
        touchActions = new TouchActions();
    }

    private void OnEnable()
    {
        // Input Actions Ȱ��ȭ
        touchActions.Enable();

        // ��ġ �̺�Ʈ ���
        touchActions.Touch.Press.started += OnTouchStarted;
    }

    private void OnDisable()
    {
        // Input Actions ��Ȱ��ȭ �� �̺�Ʈ ����
        touchActions.Touch.Press.started -= OnTouchStarted;
        touchActions.Disable();
    }

    private void Start()
    {
        // GameManager�� �ɼ� �г� ���� ����
        if (GameManager.Instance != null && optionPanel != null)
        {
            GameManager.Instance.SetStartSceneReferences(optionPanel);
        }

        // �ɼ� ��ư ������ ���
        if (optionButton != null)
        {
            optionButton.onClick.AddListener(OnOptionButtonClick);
        }

        // �ణ�� ���� �� ��ġ Ȱ��ȭ (�� ��ȯ ���� ����� ��ġ ����)
        StartCoroutine(EnableTouchAfterDelay());
    }

    private IEnumerator EnableTouchAfterDelay()
    {
        yield return new WaitForSeconds(touchDelay);
        canStartGame = true;
    }

    private void OnOptionButtonClick()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ToggleOptionPanel();
        }
    }

    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        // ���� ���� ������ �����̰� ��ȯ ���� �ƴ� ���� ó��
        if (canStartGame && !isTransitioning)
        {
            // UI ��� ������ ��ġ�� ���� ó������ ����
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            OnScreenTouch();
        }
    }

    private void OnScreenTouch()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        // ȿ���� ���
        if (SoundManager.Instance != null)
        {
            if (SoundManager.Instance.currentSoundBank == null)
            {
                SoundManager.Instance.LoadSoundBank("IntroSoundBank");
            }

            SoundManager.Instance.PlaySound("Button_sfx", 1f, false);
        }

        // �ؽ�Ʈ ������ ȿ�� ���� (�ִ� ���)
        if (touchToEngageText != null)
        {
            touchToEngageText.StopBlink();
        }

        // ȭ�� ���̵� �ƿ� ȿ���� �Բ� ���� ����
        StartCoroutine(TransitionToGameStart());
    }

    private IEnumerator TransitionToGameStart()
    {
        // ª�� ���� �ð� (ȿ����, �ð� ȿ�� ���� ���� �ð�)
        yield return new WaitForSeconds(0.3f);

        // ���� ����
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
    }

    private void OnDestroy()
    {
        // Input Actions ����
        if (touchActions != null)
        {
            touchActions.Touch.Press.started -= OnTouchStarted;
            touchActions.Disable();
            touchActions = null;
        }

        // �̺�Ʈ ������ ����
        if (optionButton != null)
        {
            optionButton.onClick.RemoveListener(OnOptionButtonClick);
        }
    }
}