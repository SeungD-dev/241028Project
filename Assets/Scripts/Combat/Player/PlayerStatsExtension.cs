using UnityEngine;

/// <summary>
/// PlayerStats Ŭ������ Ȯ���Ͽ� X-Ƽ�� ���׷��̵� �ý��ۿ� �ʿ��� ���� ���� ����� �߰��մϴ�.
/// </summary>
public static class PlayerStatsExtension
{
    /// <summary>
    /// �÷��̾� ������ ������ �縸ŭ �����մϴ�.
    /// </summary>
    /// <param name="playerStats">�÷��̾� ���� �ν��Ͻ�</param>
    /// <param name="levels">������ ���� ��</param>
    /// <returns>���� ���� ����</returns>
    public static bool SubtractLevels(this PlayerStats playerStats, int levels)
    {
        if (playerStats == null || levels <= 0)
        {
            return false;
        }

        // ���� ������ ������ �������� ū�� Ȯ��
        if (playerStats.Level <= levels)
        {
            Debug.LogWarning("������ �����Ͽ� ������ �� �����ϴ�.");
            return false;
        }

        // �� ���� ��� (�ּ� 1 ����)
        int newLevel = Mathf.Max(1, playerStats.Level - levels);

        // ����� �ʵ� ������ ���� ���÷��� ���
        var levelField = typeof(PlayerStats).GetField("level",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);

        if (levelField != null)
        {
            // �ʵ� �� ����
            levelField.SetValue(playerStats, newLevel);

            // ���� ������Ʈ �޼��� ȣ��
            var updateStatsMethod = typeof(PlayerStats).GetMethod("UpdateStats",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);

            if (updateStatsMethod != null)
            {
                updateStatsMethod.Invoke(playerStats, null);
            }

            // ���� ���� �̺�Ʈ �߻� (PlayerStats�� ���ǵ� �̺�Ʈ)
            playerStats.OnLevelUp?.Invoke(newLevel);

            Debug.Log($"�÷��̾� ������ {levels}��ŭ �����߽��ϴ�. �� ����: {newLevel}");
            return true;
        }
        else
        {
            Debug.LogError("PlayerStats�� level �ʵ忡 ������ �� �����ϴ�.");
            return false;
        }
    }
}