using UnityEditor;
using UnityEngine;
//������
[System.Serializable]
public class TimeBasedSpawnSettings
{
    public float gameTimeMinutes;  // ���� �ð�(��)
    public int spawnAmount;        // �� �ð����� ������
    public float spawnInterval;    // �� �ð����� ���� �ֱ�
}


[CreateAssetMenu(fileName = "SpawnSettings", menuName = "Game/SpawnSettings")]
public class SpawnSettingsData : ScriptableObject
{
    [Header("Spawn Settings Over Time")]
    [Tooltip("�ð��� ���� ���� ����")]
    public TimeBasedSpawnSettings[] timeSettings;

    [Header("Limits")]
    [Tooltip("�ּ� ���� ����(��)")]
    public float minSpawnInterval = 1f;
    [Tooltip("�ִ� ���� ����(��)")]
    public float maxSpawnInterval = 3f;
    [Tooltip("�ּ� ���� ��")]
    public int minSpawnAmount = 3;
    [Tooltip("�ִ� ���� ��")]
    public int maxSpawnAmount = 30;

    public (int spawnAmount, float spawnInterval) GetSettingsAtTime(float gameTime)
    {
        float gameTimeMinutes = gameTime / 60f;

        // ù ��° ���� ����
        if (gameTimeMinutes < timeSettings[0].gameTimeMinutes)
        {
            return (timeSettings[0].spawnAmount, timeSettings[0].spawnInterval);
        }

        // ������ ���� ����
        if (gameTimeMinutes >= timeSettings[timeSettings.Length - 1].gameTimeMinutes)
        {
            var lastSettings = timeSettings[timeSettings.Length - 1];
            return (lastSettings.spawnAmount, lastSettings.spawnInterval);
        }

        // ���� �ð��� �ش��ϴ� ���� ã��
        for (int i = 0; i < timeSettings.Length - 1; i++)
        {
            if (gameTimeMinutes >= timeSettings[i].gameTimeMinutes &&
                gameTimeMinutes < timeSettings[i + 1].gameTimeMinutes)
            {
                // �� �ð��� ���� ����
                float t = (gameTimeMinutes - timeSettings[i].gameTimeMinutes) /
                         (timeSettings[i + 1].gameTimeMinutes - timeSettings[i].gameTimeMinutes);

                int amount = Mathf.RoundToInt(Mathf.Lerp(
                    timeSettings[i].spawnAmount,
                    timeSettings[i + 1].spawnAmount,
                    t
                ));

                float interval = Mathf.Lerp(
                    timeSettings[i].spawnInterval,
                    timeSettings[i + 1].spawnInterval,
                    t
                );

                // �Ѱ谪 ����
                amount = Mathf.Clamp(amount, minSpawnAmount, maxSpawnAmount);
                interval = Mathf.Clamp(interval, minSpawnInterval, maxSpawnInterval);

                return (amount, interval);
            }
        }

        // ����ġ ���� ��� �⺻�� ��ȯ
        Debug.LogWarning("Unexpected state in GetSettingsAtTime");
        return (minSpawnAmount, maxSpawnInterval);
    }

    // �����Ϳ��� ���� ��ȿ�� �˻�
    private void OnValidate()
    {
        if (timeSettings == null || timeSettings.Length == 0) return;
        // �Ѱ谪 �˻�
        foreach (var setting in timeSettings)
        {
            setting.spawnAmount = Mathf.Clamp(setting.spawnAmount, minSpawnAmount, maxSpawnAmount);
            setting.spawnInterval = Mathf.Clamp(setting.spawnInterval, minSpawnInterval, maxSpawnInterval);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpawnSettingsData))]
public class SpawnSettingsDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpawnSettingsData spawnSettings = (SpawnSettingsData)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Preview", EditorStyles.boldLabel);

        float testTime = EditorGUILayout.Slider("Test Time (Minutes)", 0f, 15f, 0f);
        var settings = spawnSettings.GetSettingsAtTime(testTime * 60f);

        EditorGUILayout.LabelField($"Spawn Amount: {settings.spawnAmount}");
        EditorGUILayout.LabelField($"Spawn Interval: {settings.spawnInterval:F2}s");
    }
}
#endif