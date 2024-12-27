using UnityEngine;

public class Hunter : EnemyAI
{
    [Header("Aura Settings")]
    [SerializeField] private Transform auraTransform;  // ���� ������Ʈ�� Transform
    [SerializeField] private float rotationSpeed = 100f;  // ȸ�� �ӵ� (��/��)

    protected override void InitializeStates()
    {
        base.InitializeStates();
    }

    protected override void Update()
    {
        base.Update();

        // ���� ȸ��
        if (auraTransform != null && IsGamePlaying())
        {
            auraTransform.Rotate(Vector3.forward * (rotationSpeed * Time.deltaTime));
        }
    }
}