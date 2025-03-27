using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Image characterImage;
    [SerializeField] private Sprite[] idleSprites;
    [SerializeField] private Sprite[] walkSprites;
    [SerializeField] private float idleFrameRate = 10f;
    [SerializeField] private float walkFrameRate = 10f;
    [SerializeField] private float walkSpeed = 300f;

    [Header("UI References")]
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private Button optionButton;

    private bool isWalking = false;
    private float frameTimer;
    private int currentFrame;
    private Coroutine idleCoroutine;

    private void Start()
    {
        

        if (GameManager.Instance != null && optionPanel != null)
        {
            GameManager.Instance.SetStartSceneReferences(optionPanel);
        }
        //���� ���� �߰��� ����
        //if (SoundManager.Instance != null && !SoundManager.Instance.IsBGMPlaying("BGM_Title"))
        //{
        //    SoundManager.Instance.PlaySound("BGM_Title", 1f, true);
        //}

        if (optionButton != null)
        {
            optionButton.onClick.AddListener(OnOptionButtonClick);
        }

        idleCoroutine = StartCoroutine(PlayIdleAnimation());
    }

    private void OnOptionButtonClick()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ToggleOptionPanel();
        }
    }

    private IEnumerator PlayIdleAnimation()
    {
        while (!isWalking)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / idleFrameRate)
            {
                frameTimer = 0f;
                currentFrame = (currentFrame + 1) % idleSprites.Length;
                characterImage.sprite = idleSprites[currentFrame];
            }
            yield return null;
        }
    }

    public void OnStartButtonClick()
    {
        if (SoundManager.Instance.currentSoundBank == null)
        {
            SoundManager.Instance.LoadSoundBank("IntroSoundBank");
        }
        if (SoundManager.Instance.currentSoundBank != null)
        {
            SoundManager.Instance.PlaySound("Button_sfx", 1f, false);
        }
        if (!isWalking)
        {
            isWalking = true;
            if (idleCoroutine != null)
            {
                StopCoroutine(idleCoroutine); // Idle �ִϸ��̼� ����
            }
            StartCoroutine(WalkAndStartGame());
        }
    }

    private IEnumerator WalkAndStartGame()
    {
        RectTransform rectTransform = characterImage.rectTransform;
        currentFrame = 0;
        frameTimer = 0f;
        float targetX = 460f;
        // ���� ��ġ�� ��ǥ ��ġ�� ������ �������� �̵�
        while (rectTransform.anchoredPosition.x < targetX)
        {
            // ĳ���� �̵�
            Vector2 position = rectTransform.anchoredPosition;
            position.x += walkSpeed * Time.deltaTime;
            // ��ǥ ��ġ�� �Ѿ�� �ʵ��� ����
            position.x = Mathf.Min(position.x, targetX);
            rectTransform.anchoredPosition = position;
            // �ȱ� �ִϸ��̼� ������ ������Ʈ
            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / walkFrameRate)
            {
                frameTimer = 0f;
                currentFrame = (currentFrame + 1) % walkSprites.Length;
                characterImage.sprite = walkSprites[currentFrame];
            }
            yield return null;
        }
        // ��ǥ ��ġ�� �����ϸ� ���� ����
        GameManager.Instance.StartGame();
    }

    private void OnDestroy()
    {
        // Clean up event listeners
        if (optionButton != null)
        {
            optionButton.onClick.RemoveListener(OnOptionButtonClick);
        }
    }
}