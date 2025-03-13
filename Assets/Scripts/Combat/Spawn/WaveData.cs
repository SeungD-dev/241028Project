using UnityEngine;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "WaveData", menuName = "ScriptableObjects/WaveData")]
public class WaveData : ScriptableObject
{
    [Serializable]
    public class WaveEnemy
    {
        public EnemyData enemyData;
        [Range(0, 100)]
        public float spawnChance = 100f;
    }

    [Serializable]
    public class Wave
    {
        [Header("Wave Settings")]
        public int waveNumber;

        [Header("Time Settings")]
        public float waveDuration = 60f; // ���̺� ���� �ð�(��)
        public float survivalDuration = 15f; // �߰� ���� �ð�(��)

        [Header("Spawn Settings")]
        public float spawnInterval = 1f; // ���� ����(��)
        public int spawnAmount = 3; // �� ���� �����Ǵ� �� ��

        [Header("Enemies")]
        public List<WaveEnemy> enemies = new List<WaveEnemy>();

        [Header("Rewards")]
        public int coinReward = 10; // ���̺� Ŭ���� �� ���޵Ǵ� ����
    }

    [Header("Waves")]
    public List<Wave> waves = new List<Wave>();

    // ������ ���̺� ��ȣ�� ������ ��ȯ
    public Wave GetWave(int waveNumber)
    {
        foreach (Wave wave in waves)
        {
            if (wave.waveNumber == waveNumber)
                return wave;
        }

        // ������ ������ ���̺� ��ȯ
        return waves.Count > 0 ? waves[waves.Count - 1] : null;
    }

    // ���� ���̺� ��ȣ ��������
    public int GetNextWaveNumber(int currentWaveNumber)
    {
        int nextWaveNumber = currentWaveNumber + 1;

        foreach (Wave wave in waves)
        {
            if (wave.waveNumber == nextWaveNumber)
            {
                return nextWaveNumber;
            }
        }

        // �� �̻� ���̺갡 ������ -1 ��ȯ
        return -1;
    }

    // ���� �� ������ ��������
    public EnemyData GetRandomEnemy(Wave wave)
    {
        if (wave == null || wave.enemies.Count == 0)
            return null;

        // �� Ȯ�� ���
        float totalChance = 0f;
        foreach (var enemy in wave.enemies)
        {
            totalChance += enemy.spawnChance;
        }

        // ���� �� ����
        float random = UnityEngine.Random.Range(0, totalChance);
        float currentSum = 0f;

        // ���õ� �� ã��
        foreach (var enemy in wave.enemies)
        {
            currentSum += enemy.spawnChance;
            if (random <= currentSum)
                return enemy.enemyData;
        }

        // �⺻��
        return wave.enemies[0].enemyData;
    }

#if UNITY_EDITOR
    // ������ �̸����� ��ƿ��Ƽ
    public void PreviewWave(int waveNumber)
    {
        Wave wave = GetWave(waveNumber);
        if (wave == null)
        {
            Debug.LogWarning($"Wave {waveNumber} not found!");
            return;
        }

        Debug.Log($"Wave {waveNumber} Preview:" +
                  $"\nDuration: {wave.waveDuration}s + {wave.survivalDuration}s survival" +
                  $"\nSpawn Interval: {wave.spawnInterval}s" +
                  $"\nSpawn Amount: {wave.spawnAmount} enemies per spawn" +
                  $"\nEnemies: {wave.enemies.Count} types");

        // �� ���� Ȯ�� �м�
        float totalChance = 0;
        foreach (var enemy in wave.enemies)
        {
            totalChance += enemy.spawnChance;
        }

        foreach (var enemy in wave.enemies)
        {
            float actualChance = enemy.spawnChance / totalChance * 100;
            Debug.Log($"- {enemy.enemyData.enemyName}: {actualChance:F1}% chance");
        }
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(WaveData))]
public class WaveDataEditor : Editor
{
    private int previewWaveNumber = 1;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WaveData waveData = (WaveData)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview Tools", EditorStyles.boldLabel);

        previewWaveNumber = EditorGUILayout.IntField("Wave Number", previewWaveNumber);

        if (GUILayout.Button("Preview Wave"))
        {
            waveData.PreviewWave(previewWaveNumber);
        }
    }
}
#endif