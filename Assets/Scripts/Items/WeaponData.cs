using UnityEditor;
using UnityEngine;

[System.Serializable]
public class TierStats
{
    [Header("Stats")]
    [Tooltip("해당 티어의 기본 데미지")]
    public float damage = 10f;
    [Tooltip("해당 티어의 기본 공격 딜레이")]
    public float attackDelay = 1f;
    [Tooltip("해당 티어의 투사체 속도")]
    public float projectileSpeed = 10f;
    [Tooltip("해당 티어의 넉백 수치")]
    public float knockback = 1f;
    [Tooltip("해당 티어의 투사체 크기")]
    public float projectileSize = 1f;
    [Tooltip("해당 티어의 사거리")]
    public float range = 5f;

    [Header("Projectile Properties")]
    [Tooltip("true일 경우 투사체가 적을 관통합니다")]
    public bool canPenetrate = false;
    [Tooltip("관통 가능한 최대 적 수 (0 = 무한)")]
    public int maxPenetrationCount = 0;
    [Tooltip("관통시 데미지 감소율 (0.1 = 10% 감소)")]
    public float penetrationDamageDecay = 0.1f;

    public int projectileCount = 3;
    public float spreadAngle = 45f;
    public struct PenetrationInfo
    {
        public bool canPenetrate;
        public int maxCount;
        public float damageDecay;
    }

    public PenetrationInfo GetPenetrationInfo() => new PenetrationInfo
    {
        canPenetrate = canPenetrate,
        maxCount = maxPenetrationCount,
        damageDecay = penetrationDamageDecay
    };
}

#if UNITY_EDITOR
[CustomEditor(typeof(WeaponData))]
public class WeaponDataEditor : Editor
{
    private SerializedProperty width;
    private SerializedProperty height;
    private SerializedProperty weaponIcon;
    private SerializedProperty rarity;
    private SerializedProperty weaponType;
    private SerializedProperty price;
    private SerializedProperty weaponName;
    private SerializedProperty weaponDescription;
    private SerializedProperty sellPriceRatio;
    private SerializedProperty currentTier;
    private SerializedProperty projectilePrefab;
    private SerializedProperty tierStats;

    private void OnEnable()
    {
        width = serializedObject.FindProperty("width");
        height = serializedObject.FindProperty("height");
        weaponIcon = serializedObject.FindProperty("weaponIcon");
        rarity = serializedObject.FindProperty("rarity");
        weaponType = serializedObject.FindProperty("weaponType");
        price = serializedObject.FindProperty("price");
        weaponName = serializedObject.FindProperty("weaponName");
        weaponDescription = serializedObject.FindProperty("weaponDescription");
        sellPriceRatio = serializedObject.FindProperty("sellPriceRatio");
        currentTier = serializedObject.FindProperty("currentTier");
        projectilePrefab = serializedObject.FindProperty("projectilePrefab");
        tierStats = serializedObject.FindProperty("tierStats");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        WeaponData weaponData = (WeaponData)target;

        DrawBasicSettings();
        EditorGUILayout.Space();
        DrawTierConfiguration();
        EditorGUILayout.Space();
        DrawTierStats(weaponData);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBasicSettings()
    {
        EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(width);
        EditorGUILayout.PropertyField(height);
        EditorGUILayout.PropertyField(weaponIcon);
        EditorGUILayout.PropertyField(rarity);
        EditorGUILayout.PropertyField(weaponType);
        EditorGUILayout.PropertyField(price);
        EditorGUILayout.PropertyField(weaponName);
        EditorGUILayout.PropertyField(weaponDescription);
        EditorGUILayout.PropertyField(sellPriceRatio);
    }

    private void DrawTierConfiguration()
    {
        EditorGUILayout.LabelField("Tier Configuration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(currentTier);
        EditorGUILayout.PropertyField(projectilePrefab);
    }

    private void DrawTierStats(WeaponData weaponData)
    {
        EditorGUILayout.LabelField("Tier Stats", EditorStyles.boldLabel);

        EditorGUI.indentLevel++;
        for (int i = 0; i < tierStats.arraySize; i++)
        {
            SerializedProperty tierStat = tierStats.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Tier {i + 1}", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Basic Stats", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("damage"));
            EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("attackDelay"));
            EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("projectileSpeed"));
            EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("knockback"));
            EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("projectileSize"));
            EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("range"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Projectile Properties", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("canPenetrate"));
            EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("maxPenetrationCount"));
            EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("penetrationDamageDecay"));

            // Shotgun 타입일 때만 추가 속성 표시
            if (weaponData.weaponType == WeaponType.Shotgun)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Shotgun Properties", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("projectileCount"),
                    new GUIContent("Projectile Count", "샷건의 발사 투사체 수"));
                EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("spreadAngle"),
                    new GUIContent("Spread Angle", "샷건의 발사 각도 범위 (도)"));
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        EditorGUI.indentLevel--;
    }
}
#endif
[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Settings")]
    public int width = 1;
    public int height = 1;
    public Sprite weaponIcon;
    public WeaponRarity rarity;
    public WeaponType weaponType;
    public int price;
    public string weaponName;
    public string weaponDescription;
    [SerializeField] private float sellPriceRatio = 0.5f;
    public int SellPrice => Mathf.RoundToInt(price * sellPriceRatio);

    [Header("Tier Configuration")]
    [Tooltip("현재 무기의 티어")]
    public int currentTier = 1;

    [Tooltip("각 티어별 스탯 설정")]
    public TierStats[] tierStats = new TierStats[4]; // 1-4 티어

    [Header("Prefabs")]
    public GameObject projectilePrefab;

    // 현재 티어의 스탯 getter
    public TierStats CurrentTierStats => tierStats[Mathf.Clamp(currentTier - 1, 0, 3)];

    // PlayerStats를 고려한 최종 스탯 계산 메서드들
    public float CalculateFinalDamage(PlayerStats playerStats)
    {
        if (playerStats == null) return CurrentTierStats.damage;

        float baseDamage = CurrentTierStats.damage;
        float powerMultiplier = 1f + (playerStats.Power / 100f);

        return baseDamage * powerMultiplier;
    }

    public float CalculateFinalAttackDelay(PlayerStats playerStats)
    {
        if (playerStats == null) return CurrentTierStats.attackDelay;

        float baseDelay = CurrentTierStats.attackDelay;
        float cooldownReduction = Mathf.Min(playerStats.CooldownReduce, 90f);

        return baseDelay * (1f - (cooldownReduction / 100f));
    }

    public float CalculateFinalKnockback(PlayerStats playerStats)
    {
        if (playerStats == null) return CurrentTierStats.knockback;

        float baseKnockback = CurrentTierStats.knockback;
        float knockbackMultiplier = 1f + (playerStats.Knockback / 100f);

        return baseKnockback * knockbackMultiplier;
    }

    public float CalculateFinalProjectileSize(PlayerStats playerStats)
    {
        if (playerStats == null) return CurrentTierStats.projectileSize;

        float baseSize = CurrentTierStats.projectileSize;
        float aoeMultiplier = 1f + (playerStats.AreaOfEffect / 100f);

        return baseSize * aoeMultiplier;
    }

    public float CalculateFinalRange(PlayerStats playerStats)
    {
        if (playerStats == null) return CurrentTierStats.range;
        return CurrentTierStats.range;  // 사거리는 플레이어 스탯의 영향을 받지 않음
    }

    // 관통 정보 getter
    public TierStats.PenetrationInfo GetPenetrationInfo()
    {
        return CurrentTierStats.GetPenetrationInfo();
    }

    public float CalculateAttacksPerSecond(PlayerStats playerStats)
    {
        return 1f / CalculateFinalAttackDelay(playerStats);
    }

    // DPS 계산 (UI 표시용)
    public float CalculateTheoreticalDPS(PlayerStats playerStats)
    {
        float damage = CalculateFinalDamage(playerStats);
        float attackDelay = CalculateFinalAttackDelay(playerStats);

        return damage / attackDelay;
    }

    // 다음 티어 무기 생성
    public WeaponData CreateNextTierWeapon()
    {
        if (currentTier >= 4) return null;

        WeaponData nextTierWeapon = Instantiate(this);
        nextTierWeapon.currentTier = currentTier + 1;
        nextTierWeapon.weaponName = $"{weaponName} Tier {nextTierWeapon.currentTier}";

        return nextTierWeapon;
    }

    private void OnValidate()
    {
        if (tierStats == null || tierStats.Length != 4)
        {
            TierStats[] newTierStats = new TierStats[4];
            if (tierStats != null)
            {
                for (int i = 0; i < Mathf.Min(tierStats.Length, 4); i++)
                {
                    newTierStats[i] = tierStats[i] ?? new TierStats();
                }
            }
            for (int i = (tierStats?.Length ?? 0); i < 4; i++)
            {
                newTierStats[i] = new TierStats();
            }
            tierStats = newTierStats;
        }

        for (int i = 0; i < 4; i++)
        {
            if (tierStats[i] == null)
            {
                tierStats[i] = new TierStats();
            }
        }
        if (weaponType == WeaponType.Shotgun)
        {
            for (int i = 0; i < tierStats.Length; i++)
            {
                if (tierStats[i] == null) continue;

                // 기본값이 아직 설정되지 않은 경우에만 설정
                if (tierStats[i].projectileCount <= 0)
                {
                    tierStats[i].projectileCount = 3 + i;  // 1티어: 3발, 2티어: 4발, ...
                }
                if (tierStats[i].spreadAngle <= 0)
                {
                    tierStats[i].spreadAngle = 45f + (i * 5f);  // 1티어: 45도, 2티어: 50도, ...
                }
            }
        }
    }
}