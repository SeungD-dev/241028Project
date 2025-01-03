using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class WeaponInfoUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI weaponLevelText;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI weaponDescriptionText;

    [Header("Upgrade System")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI upgradeButtonText;
    [SerializeField] private WeaponDatabase weaponDatabase;

    [Header("Scene References")]
    [SerializeField] private InventoryController inventoryController;

    [Header("Grid References")]
    [SerializeField] private ItemGrid mainItemGrid;

    private PlayerStats playerStats;
    private WeaponData selectedWeapon;
    private List<InventoryItem> upgradeableWeapons;
    private bool isInitialized = false;

    private void Start()
    {
        ValidateReferences();

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(OnUpgradeButtonClick);
            upgradeButton.gameObject.SetActive(false);
        }

        // Grid �̺�Ʈ ������ ���
        if (mainItemGrid != null)
        {
            mainItemGrid.OnGridChanged += RefreshUpgradeUI;
        }

        if (GameManager.Instance.IsInitialized)
        {
            InitializeReferences();
        }

        GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
    }

    private void ValidateReferences()
    {
        if (weaponLevelText == null) Debug.LogError($"Missing reference: {nameof(weaponLevelText)} in {gameObject.name}");
        if (weaponNameText == null) Debug.LogError($"Missing reference: {nameof(weaponNameText)} in {gameObject.name}");
        if (weaponDescriptionText == null) Debug.LogError($"Missing reference: {nameof(weaponDescriptionText)} in {gameObject.name}");
        if (upgradeButton == null) Debug.LogError($"Missing reference: {nameof(upgradeButton)} in {gameObject.name}");
        if (upgradeButtonText == null) Debug.LogError($"Missing reference: {nameof(upgradeButtonText)} in {gameObject.name}");
        if (weaponDatabase == null) Debug.LogError($"Missing reference: {nameof(weaponDatabase)} in {gameObject.name}");
        if (mainItemGrid == null) Debug.LogError($"Missing reference: {nameof(mainItemGrid)} in {gameObject.name}");
        if (inventoryController == null) Debug.LogError($"Missing reference: {nameof(inventoryController)} in {gameObject.name}");
    }


    private void InitializeReferences()
    {
        if (isInitialized) return;

        playerStats = GameManager.Instance.PlayerStats;

        if (playerStats != null && inventoryController != null)
        {
            isInitialized = true;
            if (selectedWeapon != null)
            {
                UpdateWeaponInfo(selectedWeapon);
            }
        }
    }
    public void RefreshUpgradeUI()
    {
        if (selectedWeapon != null)
        {
            CheckUpgradePossibility();
        }
    }
    private void OnGameStateChanged(GameState newState)
    {
        if (!isInitialized && GameManager.Instance.IsInitialized)
        {
            InitializeReferences();
        }
    }

    public void UpdateWeaponInfo(WeaponData weaponData)
    {
        if (weaponData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        selectedWeapon = weaponData;
        gameObject.SetActive(true);

        weaponLevelText.text = $"Tier {weaponData.currentTier}";
        weaponNameText.text = weaponData.weaponName;
        
        weaponDescriptionText.text = weaponData.weaponDescription;

        if (isInitialized && playerStats != null)
        {
            UpdateDetailedStats(weaponData);
            CheckUpgradePossibility();
        }
    }
    private void UpdateDetailedStats(WeaponData weaponData)
    {
        float dps = weaponData.CalculateTheoreticalDPS(playerStats);
        float damage = weaponData.CalculateFinalDamage(playerStats);
        float attacksPerSecond = weaponData.CalculateAttacksPerSecond(playerStats);
        float knockbackPower = weaponData.CalculateFinalKnockback(playerStats);
        float range = weaponData.CalculateFinalRange(playerStats);
        float projectileSize = weaponData.CalculateFinalProjectileSize(playerStats);
        var penetrationInfo = weaponData.GetPenetrationInfo();

        string penetrationText = penetrationInfo.canPenetrate ?
            $"����: {(penetrationInfo.maxCount == 0 ? "������" : penetrationInfo.maxCount.ToString())}ȸ" :
            "����: �Ұ�";

        string statText = $"DPS: {dps:F1}\n" +
                         $"DMG: {damage:F1}\n" +
                         $"ASPD: {attacksPerSecond:F2}/s\n" +
                         $"Range: {range:F1}\n" +
                         $"Size: {projectileSize:F1}x\n" +
                         $"KnockBack: {knockbackPower:F1}x\n" +
                         penetrationText;
    }

    private void CheckUpgradePossibility()
    {
        if (!isInitialized || selectedWeapon == null || mainItemGrid == null || upgradeButton == null)
        {
            Debug.LogWarning($"CheckUpgradePossibility failed: initialized={isInitialized}, selectedWeapon={selectedWeapon != null}, mainItemGrid={mainItemGrid != null}, upgradeButton={upgradeButton != null}");
            return;
        }

        upgradeableWeapons = new List<InventoryItem>();

        // ���� ���õ� ������ ���� ����
        WeaponType targetType = selectedWeapon.weaponType;
        int targetTier = selectedWeapon.currentTier;

        // Equipment Ÿ���� ��� �߰��� üũ�� equipmentType
        EquipmentType targetEquipmentType = EquipmentType.None;
        if (targetType == WeaponType.Equipment)
        {
            targetEquipmentType = selectedWeapon.equipmentType;
        }

        // Grid�� ��� �������� �˻�
        for (int x = 0; x < mainItemGrid.Width; x++)
        {
            for (int y = 0; y < mainItemGrid.Height; y++)
            {
                InventoryItem item = mainItemGrid.GetItem(x, y);
                if (item != null && item.weaponData != null)
                {
                    bool isMatchingType = false;

                    if (targetType == WeaponType.Equipment)
                    {
                        // Equipment�� ��� WeaponType�� EquipmentType ��� üũ
                        isMatchingType = item.weaponData.weaponType == targetType &&
                                       item.weaponData.equipmentType == targetEquipmentType;
                    }
                    else
                    {
                        // �Ϲ� ������ ��� WeaponType�� üũ
                        isMatchingType = item.weaponData.weaponType == targetType;
                    }

                    if (isMatchingType &&
                        item.weaponData.currentTier == targetTier &&
                        !upgradeableWeapons.Contains(item))
                    {
                        upgradeableWeapons.Add(item);
                    }
                }
            }
        }

        bool canUpgrade = upgradeableWeapons.Count >= 2 && targetTier < 4;
        upgradeButton.gameObject.SetActive(canUpgrade);

        if (canUpgrade)
        {
            upgradeButtonText.text = $"Upgrade to Tier {targetTier + 1}";
        }
    }
    private void OnUpgradeButtonClick()
    {
        if (!isInitialized || upgradeableWeapons == null || upgradeableWeapons.Count < 2 || selectedWeapon == null || mainItemGrid == null)
        {
            Debug.LogWarning("Cannot upgrade: missing requirements");
            return;
        }

        WeaponData nextTierWeapon = GetNextTierWeapon();
        if (nextTierWeapon == null)
        {
            Debug.LogWarning("Failed to create next tier weapon");
            return;
        }

        // ���� ������� ��ġ�� ����
        Vector2Int upgradePosition = new Vector2Int(
            upgradeableWeapons[0].onGridPositionX,
            upgradeableWeapons[0].onGridPositionY
        );

        // Equipment ȿ�� ����
        if (selectedWeapon.weaponType == WeaponType.Equipment)
        {
            var weaponManager = GameObject.FindGameObjectWithTag("Player")?.GetComponent<WeaponManager>();
            if (weaponManager != null)
            {
                // ���׷��̵忡 ���Ǵ� �� ����� ȿ���� ����
                foreach (var weapon in upgradeableWeapons.Take(2))
                {
                    weaponManager.UnequipWeapon(weapon.weaponData);
                }
            }
        }

        // ���� ����� ����
        foreach (var weapon in upgradeableWeapons.Take(2))
        {
            if (weapon != null)
            {
                mainItemGrid.PickUpItem(weapon.onGridPositionX, weapon.onGridPositionY);
                Destroy(weapon.gameObject);
            }
        }

        // �� ���� ���� �� ��ġ
        upgradeableWeapons.Clear();
        selectedWeapon = null;
        if (inventoryController != null)
        {
            inventoryController.CreateUpgradedItem(nextTierWeapon, upgradePosition);
        }

        upgradeButton.gameObject.SetActive(false);
    }
    private WeaponData GetNextTierWeapon()
    {
        if (selectedWeapon == null || selectedWeapon.currentTier >= 4) return null;
        return selectedWeapon.CreateNextTierWeapon();
    }  
    private void OnDestroy()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(OnUpgradeButtonClick);
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
        if (mainItemGrid != null)
        {
            mainItemGrid.OnGridChanged -= RefreshUpgradeUI;
        }
    }
}