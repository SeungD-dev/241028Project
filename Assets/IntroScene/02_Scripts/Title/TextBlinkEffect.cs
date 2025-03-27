using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextBlinkEffect : MonoBehaviour
{
    [Header("������ ����")]
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private float blinkInterval = 0.5f; // ������ ���� (�� ����)
    [SerializeField] private float visibleDuration = 0.3f; // �ؽ�Ʈ��
    [SerializeField] private float invisibleDuration = 0.2f; // �ؽ�Ʈ�� ������ �ʴ� �ð�
    [SerializeField] private bool startBlinkOnAwake = true; // ���� �� �ڵ� ������ ����

    private Coroutine blinkCoroutine;
    private bool isBlinking = false;

    void Awake()
    {
        // Ÿ�� �ؽ�Ʈ�� �������� �ʾҴٸ� ���� ���ӿ�����Ʈ���� ã��
        if (targetText == null)
        {
            targetText = GetComponent<TextMeshProUGUI>();
        }

        if (targetText == null)
        {
            Debug.LogError("TextBlinkEffect: TextMeshProUGUI ������Ʈ�� ã�� �� �����ϴ�.");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        if (startBlinkOnAwake)
        {
            StartBlink();
        }
    }

    void OnDisable()
    {
        StopBlink();
    }

    /// <summary>
    /// ������ ȿ�� ����
    /// </summary>
    public void StartBlink()
    {
        if (isBlinking || targetText == null) return;

        isBlinking = true;
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    /// <summary>
    /// ������ ȿ�� ����
    /// </summary>
    public void StopBlink()
    {
        if (!isBlinking) return;

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        // �ؽ�Ʈ ���̰� ����
        if (targetText != null)
        {
            targetText.enabled = true;
        }

        isBlinking = false;
    }

    /// <summary>
    /// ������ ���ݰ� ���� �ð� ����
    /// </summary>
    public void SetBlinkTiming(float interval, float visibleTime, float invisibleTime)
    {
        blinkInterval = Mathf.Max(0.1f, interval);
        visibleDuration = Mathf.Max(0.01f, visibleTime);
        invisibleDuration = Mathf.Max(0.01f, invisibleTime);

        // �̹� �������� ���� ���̸� ������Ͽ� �� ���� ����
        if (isBlinking)
        {
            StopBlink();
            StartBlink();
        }
    }

    private IEnumerator BlinkRoutine()
    {
        WaitForSeconds visibleWait = new WaitForSeconds(visibleDuration);
        WaitForSeconds invisibleWait = new WaitForSeconds(invisibleDuration);
        WaitForSeconds intervalWait = new WaitForSeconds(blinkInterval);

        while (isBlinking)
        {
            // ���� �������� ������

            // 1. �ؽ�Ʈ ���̱�
            targetText.enabled = true;
            yield return visibleWait;

            // 2. �ؽ�Ʈ �����
            targetText.enabled = false;
            yield return invisibleWait;

            // 3. ���� ������ ����Ŭ �� ���
            targetText.enabled = true;
            yield return intervalWait;
        }
    }
}