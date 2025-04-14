using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// X-Ƽ�� ���� ���׷��̵� UI�� �����ϴ� Ŭ����
/// ���̺� Ŭ���� �� 4Ƽ�� ���⸦ X-Ƽ��� ���׷��̵��ϴ� UI�� �����մϴ�.
/// </summary>
public class EnhancedWeaponUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI levelInfoText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button skipButton;
    [SerializeField] private Transform weaponOptionContainer;
    [SerializeField] private GameObject weaponOptionPrefab;

    [Header("Enhanced Weapon Panel")]
    [SerializeField] private EnhancedWeaponManager enhancedWeaponManager;

    // ���� ����
    private List<WeaponData> availableWeapons = new List<WeaponData>();
    private int playerLevel;
    private int levelCost;
    private List<GameObject> instantiatedOptions = new List<GameObject>();

    private void Awake()
    {
        InitializeComponents();
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    /// <summary>
    /// �ʿ��� ������Ʈ ���� �ʱ�ȭ
    /// </summary>
    private void InitializeComponents()
    {
        if (enhancedWeaponManager == null)
        {
            enhancedWeaponManager = FindAnyObjectByType<EnhancedWeaponManager>();
        }

        // �⺻ �ؽ�Ʈ ����
        if (titleText != null)
        {
            titleText.text = "X-TIER WEAPON UPGRADE";
        }

        if (descriptionText != null)
        {
            descriptionText.text = "Select one weapon to upgrade to X-Tier. The upgrade will cost player levels.";
        }
    }

    /// <summary>
    /// ���׷��̵� ������ ���� ������ ����
    /// </summary>
    public void SetWeaponsData(List<WeaponData> weapons)
    {
        availableWeapons = new List<WeaponData>(weapons);
    }

    /// <summary>
    /// ���� �÷��̾� ���� ����
    /// </summary>
    public void SetPlayerLevel(int level)
    {
        playerLevel = level;
    }

    /// <summary>
    /// ���� ��� ����
    /// </summary>
    public void SetLevelCost(int cost)
    {
        levelCost = cost;
    }

    /// <summary>
    /// UI ����
    /// </summary>
    private void UpdateUI()
    {
        // ���� ���� ����
        if (levelInfoText != null)
        {
            levelInfoText.text = $"Your Level: {playerLevel} / Cost: {levelCost} Levels";
        }

        // ���� �ɼ� ����
        CreateWeaponOptions();
    }

    /// <summary>
    /// ���� �ɼ� UI ����
    /// </summary>
    private void CreateWeaponOptions()
    {
        // ���� �ɼ� ����
        ClearWeaponOptions();

        if (weaponOptionContainer == null || weaponOptionPrefab == null)
        {
            Debug.LogError("���� �ɼ� �����̳� �Ǵ� �������� �������� �ʾҽ��ϴ�.");
            return;
        }

        // �� ���⸶�� �ɼ� UI ����
        foreach (var weaponData in availableWeapons)
        {
            if (weaponData == null) continue;

            GameObject optionObj = Instantiate(weaponOptionPrefab, weaponOptionContainer);
            instantiatedOptions.Add(optionObj);

            // EnhancedWeaponOption ������Ʈ ��������
            EnhancedWeaponOption option = optionObj.GetComponent<EnhancedWeaponOption>();
            if (option != null)
            {
                option.Initialize(weaponData, this);
            }
        }
    }

    /// <summary>
    /// ���� ���� �ɼ� ����
    /// </summary>
    private void ClearWeaponOptions()
    {
        foreach (var option in instantiatedOptions)
        {
            if (option != null)
            {
                Destroy(option);
            }
        }

        instantiatedOptions.Clear();
    }

    /// <summary>
    /// ���� ���� ó��
    /// </summary>
    public void OnWeaponSelected(WeaponData weaponData)
    {
        if (weaponData == null || enhancedWeaponManager == null) return;

        // ������ ���⸦ X-Ƽ��� ���׷��̵�
        enhancedWeaponManager.UpgradeToXTier(weaponData);
    }
  
    private void OnDestroy()
    {
        ClearWeaponOptions();
    }
}