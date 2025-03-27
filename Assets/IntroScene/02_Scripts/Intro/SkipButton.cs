using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SkipButton : MonoBehaviour
{
    [SerializeField] private Button skipButton;
    [SerializeField] private float skipButtonActiveTime = 3.0f;
    public string nextSceneName; // ��ŵ ��ư ��ġ �� ��ȯ�� �� �̸�
    private Coroutine hideButtonCoroutine;

    void Start()
    {
        skipButton.gameObject.SetActive(false);
        skipButton.onClick.AddListener(OnClickSkipButton);
    }

    void Update()
    {
        // ȭ�� �ƹ� ���̳� ��ġ �� ��ŵ ��ư Ȱ��ȭ
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            ActivateSkipButton();
        }
    }

    private void OnEnable()
    {
        // Ȱ��ȭ�� �� ���� �ð�(skipButtonActiveTime) �� ��ŵ ��ư ��Ȱ��ȭ(�ڷ�ƾ)
        if (skipButton.gameObject.activeSelf)
        {
            if (hideButtonCoroutine != null)
            {
                StopCoroutine(hideButtonCoroutine);
            }
            hideButtonCoroutine = StartCoroutine(HideButtonAfterDelay());
        }
    }

    private void ActivateSkipButton()
    {
        skipButton.gameObject.SetActive(true);

        if (hideButtonCoroutine != null)
        {
            StopCoroutine(hideButtonCoroutine);
        }
        hideButtonCoroutine = StartCoroutine(HideButtonAfterDelay());
    }

    private IEnumerator HideButtonAfterDelay()
    {
        yield return new WaitForSeconds(skipButtonActiveTime);
        skipButton.gameObject.SetActive(false);
        hideButtonCoroutine = null;
    }

    /// <summary>
    /// ���� ������ �̵�
    /// </summary>
    void OnClickSkipButton()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}