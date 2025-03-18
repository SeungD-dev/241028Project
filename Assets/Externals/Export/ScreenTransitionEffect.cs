using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System;

public class ScreenTransitionEffect : MonoBehaviour
{
    [Header("Ʈ������ ����")]
    public float transitionDuration = 0.5f;
    public Ease scaleEase = Ease.InOutQuad;
    public bool useBlackScreen = true;

    [Header("������ ����")]
    public bool autoRevert = false; // ����Ʈ ������ �ٷ� reverDelay�Ŀ� �ٽ� ȿ�� �����Ǽ� �״�� ��µǰ� �Ұ�����
    public float revertDelay = 0.2f;
    public bool reverseEffect = false; // ȿ�� ���� ���� ����

    private RectTransform rectTransform;
    private Image image;
    private Vector3 originalScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        originalScale = rectTransform.localScale;

        
        gameObject.SetActive(false);
    }

    // �׽�Ʈ �ڵ� ���� - Update �޼��� ��ü ����

    public void PlayTransition(Action onTransitionComplete = null)
    {
        Debug.Log("PlayTransition called");

        // ���� Tween ����
        DOTween.Kill(rectTransform);

        // �ʱ� ���·� ����
        if (reverseEffect)
        {
            // ���� ȿ��: X �������� 0���� ����
            rectTransform.localScale = new Vector3(0, originalScale.y, originalScale.z);
        }
        else
        {
            // ���� ȿ��: �⺻ ũ�⿡�� ����
            rectTransform.localScale = originalScale;
        }

        gameObject.SetActive(true);

        // ������ �̹����� ����ϴ� ���
        if (useBlackScreen && image != null)
        {
            image.color = Color.black;
        }

        if (reverseEffect)
        {
            // ���� ȿ��: X �������� 0���� ���� ũ��� Ŀ��
            rectTransform.DOScaleX(originalScale.x, transitionDuration)
                .SetEase(scaleEase)
                .SetUpdate(true) // Ÿ�ӽ����� ���� ���� �ʰ�
                .OnComplete(() => {
                    // Ʈ������ �Ϸ� �� �ݹ� ȣ��
                    onTransitionComplete?.Invoke();

                    // ���⿡ ��Ȱ��ȭ �ڵ� �߰�
                    gameObject.SetActive(false);

                    // �ڵ� ������ Ȱ��ȭ�� ���
                    if (autoRevert)
                    {
                        DOVirtual.DelayedCall(revertDelay, () => {
                            RevertTransition();
                        }).SetUpdate(true);
                    }
                });
        }
        else
        {
            // ���� ȿ��: X �������� ���� ũ�⿡�� 0���� �پ��
            rectTransform.DOScaleX(0, transitionDuration)
                .SetEase(scaleEase)
                .SetUpdate(true) // Ÿ�ӽ����� ���� ���� �ʰ�
                .OnComplete(() => {
                    // Ʈ������ �Ϸ� �� �ݹ� ȣ��
                    onTransitionComplete?.Invoke();

                    // ���⿡ ��Ȱ��ȭ �ڵ� �߰�
                    gameObject.SetActive(false);

                    // �ڵ� ������ Ȱ��ȭ�� ���
                    if (autoRevert)
                    {
                        DOVirtual.DelayedCall(revertDelay, () => {
                            RevertTransition();
                        }).SetUpdate(true);
                    }
                });
        }
    }
    public void RevertTransition(Action onRevertComplete = null)
    {
        if (reverseEffect)
        {
            // ���� ȿ���� ����: X �������� ���� ũ�⿡�� 0���� �پ��
            rectTransform.DOScaleX(0, transitionDuration)
                .SetEase(scaleEase)
                .OnComplete(() => {
                    onRevertComplete?.Invoke();
                    gameObject.SetActive(false); // Ʈ������ �Ϸ� �� ��Ȱ��ȭ
                });
        }
        else
        {
            // ���� ȿ���� ����: X �������� 0���� ���� ũ��� Ŀ��
            rectTransform.DOScaleX(originalScale.x, transitionDuration)
                .SetEase(scaleEase)
                .OnComplete(() => {
                    onRevertComplete?.Invoke();
                    gameObject.SetActive(false); // Ʈ������ �Ϸ� �� ��Ȱ��ȭ
                });
        }
    }

    private void OnDisable()
    {
        DOTween.Kill(rectTransform);
    }
}