using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class TierProbability
{
    [Header("Base Probabilities")]
    [Range(0, 100)]
    public float tier1Probability = 75f;
    [Range(0, 100)]
    public float tier2Probability = 15f;
    [Range(0, 100)]
    public float tier3Probability = 9f;
    [Range(0, 100)]
    public float tier4Probability = 1f;

    [Header("Level Scaling (per level)")]
    [Range(-5, 0)]
    public float tier1Decrease = -2f;  // ������ ������
    [Range(-2, 2)]
    public float tier2Scaling = 1f;    // ������ ��ȭ��
    [Range(-2, 2)]
    public float tier3Scaling = 0.8f;  // ������ ��ȭ��
    [Range(-1, 1)]
    public float tier4Scaling = 0.2f;  // ������ ��ȭ��

    [Header("Min/Max Probabilities")]
    [Range(0, 100)] public float tier1MinProb = 20f;  // 1Ƽ�� �ּ� Ȯ��
    [Range(0, 100)] public float tier2MinProb = 10f;  // 2Ƽ�� �ּ� Ȯ��
    [Range(0, 100)] public float tier3MinProb = 5f;   // 3Ƽ�� �ּ� Ȯ��
    [Range(0, 100)] public float tier4MinProb = 1f;   // 4Ƽ�� �ּ� Ȯ��

    [Range(0, 100)] public float tier1MaxProb = 75f;  // 1Ƽ�� �ִ� Ȯ��
    [Range(0, 100)] public float tier2MaxProb = 40f;  // 2Ƽ�� �ִ� Ȯ��
    [Range(0, 100)] public float tier3MaxProb = 30f;  // 3Ƽ�� �ִ� Ȯ��
    [Range(0, 100)] public float tier4MaxProb = 15f;  // 4Ƽ�� �ִ� Ȯ��

    public float[] GetTierProbabilities(int playerLevel)
    {
        float[] probs = new float[4];

        // ������ ���� �⺻ Ȯ�� ���
        probs[0] = tier1Probability + (tier1Decrease * (playerLevel - 1));
        probs[1] = tier2Probability + (tier2Scaling * (playerLevel - 1));
        probs[2] = tier3Probability + (tier3Scaling * (playerLevel - 1));
        probs[3] = tier4Probability + (tier4Scaling * (playerLevel - 1));

        // �ּ�/�ִ� Ȯ�� ����
        probs[0] = Mathf.Clamp(probs[0], tier1MinProb, tier1MaxProb);
        probs[1] = Mathf.Clamp(probs[1], tier2MinProb, tier2MaxProb);
        probs[2] = Mathf.Clamp(probs[2], tier3MinProb, tier3MaxProb);
        probs[3] = Mathf.Clamp(probs[3], tier4MinProb, tier4MaxProb);

        // ���� ���
        float total = probs[0] + probs[1] + probs[2] + probs[3];

        // 100%�� ����ȭ
        for (int i = 0; i < 4; i++)
        {
            probs[i] = (probs[i] / total) * 100f;
        }

        return probs;
    }

    public float GetTierProbability(int tier, int playerLevel)
    {
        float[] probs = GetTierProbabilities(playerLevel);
        return tier switch
        {
            1 => probs[0],
            2 => probs[1],
            3 => probs[2],
            4 => probs[3],
            _ => 0f
        };
    }

#if UNITY_EDITOR
    // ��ȿ�� �˻縦 ���� OnValidate
    private void OnValidate()
    {
        // �ʱ� Ȯ���� ���� 100�� �ǵ��� ����
        float total = tier1Probability + tier2Probability + tier3Probability + tier4Probability;
        if (Mathf.Abs(total - 100f) > 0.01f)
        {
            float scale = 100f / total;
            tier1Probability *= scale;
            tier2Probability *= scale;
            tier3Probability *= scale;
            tier4Probability *= scale;
        }

        // �ּ�/�ִ� Ȯ�� ���� ��ȿ�� �˻�
        tier1MinProb = Mathf.Min(tier1MinProb, tier1MaxProb);
        tier2MinProb = Mathf.Min(tier2MinProb, tier2MaxProb);
        tier3MinProb = Mathf.Min(tier3MinProb, tier3MaxProb);
        tier4MinProb = Mathf.Min(tier4MinProb, tier4MaxProb);
    }
#endif
}
[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Inventory/WeaponDatabase")]
public class WeaponDatabase : ScriptableObject
{
    public List<WeaponData> weapons;
    public TierProbability tierProbability;
}