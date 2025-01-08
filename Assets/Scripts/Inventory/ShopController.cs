using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ShopController : MonoBehaviour
{
    [Header("Refresh Settings")]
    [SerializeField] private int initialRefreshCost = 5;
    [SerializeField] private int refreshCostIncrease = 1;
    private int currentRefreshCost;

    [Header("References")]
    [SerializeField] private WeaponDatabase weaponDatabase;
    [SerializeField] private WeaponOptionUI[] weaponOptions;
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private GameObject shopUI;
    [SerializeField] private GameObject playerControlUI;
    [SerializeField] private GameObject playerStatsUI;
    [Header("UI Texts")]
    [SerializeField] private TMPro.TextMeshProUGUI refreshCostText;
    [SerializeField] private TMPro.TextMeshProUGUI playerCoinsText;

    private bool isFirstShop = true;
    private bool hasFirstPurchase = false;
    private PlayerStats playerStats;
    private HashSet<WeaponData> purchasedWeapons = new HashSet<WeaponData>();

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnCoinChanged -= UpdatePlayerCoinsText;
        }
    }

    private void UpdatePlayerCoinsText(int coins)
    {
        if (playerCoinsText != null)
        {
            playerCoinsText.text = $"{coins}";
        }
    }

    private void UpdateRefreshCostText()
    {
        if (refreshCostText != null)
        {
            refreshCostText.text = $"Reroll\n{currentRefreshCost}";
        }
    }

    // UI ��ȯ �޼���
    public void OpenInventory()
    {
        SoundManager.Instance.PlaySound("Button_sfx", 1f, false);
        shopUI.SetActive(false);
        if (inventoryController != null)
        {
            inventoryController.OpenInventory();
        }
    }

    public void OpenShop()
    {
        playerControlUI.SetActive(false);
        playerStatsUI.SetActive(false);
        shopUI.SetActive(true);
        inventoryUI.SetActive(false);
        UpdateRefreshCostText();
    }

    private void InitializeWeaponOption(WeaponOptionUI option, WeaponData weapon)
    {
        if (option == null || weapon == null) return;

        // �̹� ������ �������� Ȯ��
        if (purchasedWeapons.Contains(weapon))
        {
            option.SetPurchased(true);  // WeaponOptionUI�� SetPurchased �޼��� �ʿ�
            return;
        }

        // ù �������� ���� ù ���Ű� ������ ���� ����
        if (isFirstShop && !hasFirstPurchase)
        {
            weapon.price = 0;
        }

        option.Initialize(weapon, this);
    }

    public void InitializeShop()
    {
        // ������ ���� ���� ������ �������� ��� �ʱ�ȭ
        currentRefreshCost = initialRefreshCost;
        UpdateRefreshCostText();

        // PlayerStats ���� ��������
        if (playerStats == null)
        {
            playerStats = GameManager.Instance.PlayerStats;
            if (playerStats == null)
            {
                Debug.LogError("PlayerStats reference is null in ShopController!");
                return;
            }

            // PlayerStats �̺�Ʈ ����
            playerStats.OnCoinChanged += UpdatePlayerCoinsText;
        }

        // �ʱ� ���� ǥ��
        UpdatePlayerCoinsText(playerStats.CoinCount);

        playerControlUI.SetActive(false);
        playerStatsUI.SetActive(false);
        shopUI.SetActive(true);

        if (weaponOptions == null || weaponOptions.Length == 0)
        {
            Debug.LogError("No weapon options assigned to ShopUI!");
            return;
        }

        List<WeaponData> randomWeapons = GetRandomWeapons(weaponOptions.Length);

        for (int i = 0; i < weaponOptions.Length; i++)
        {
            if (i < randomWeapons.Count && weaponOptions[i] != null)
            {
                WeaponData weapon = randomWeapons[i];
                InitializeWeaponOption(weaponOptions[i], weapon);
            }
        }

        isFirstShop = false;
    }

    private List<WeaponData> GetRandomWeapons(int count)
    {
        if (weaponDatabase == null || playerStats == null)
        {
            Debug.LogError("WeaponDatabase or PlayerStats is missing!");
            return new List<WeaponData>();
        }

        List<WeaponData> randomWeapons = new List<WeaponData>();

        for (int i = 0; i < count; i++)
        {
            WeaponData weapon = GetRandomWeaponByTierProbability();
            if (weapon != null)
            {
                randomWeapons.Add(weapon);
            }
        }

        return randomWeapons;
    }

    private WeaponData GetRandomWeaponByTierProbability()
    {
        float[] tierProbs = weaponDatabase.tierProbability.GetTierProbabilities(playerStats.Level);
        float random = Random.value * 100f;
        float cumulative = 0f;
        int selectedTier = 1;

        for (int i = 0; i < 4; i++)
        {
            cumulative += tierProbs[i];
            if (random <= cumulative)
            {
                selectedTier = i + 1;
                break;
            }
        }

        List<WeaponData> tierWeapons = weaponDatabase.weapons
            .Where(w => w.currentTier == selectedTier)
            .Where(w => !isFirstShop || w.weaponType != WeaponType.Equipment)
            .ToList();

        if (tierWeapons.Count == 0)
        {
            Debug.LogWarning($"No weapons found for tier {selectedTier}");
            return null;
        }

        return ScriptableObject.Instantiate(tierWeapons[Random.Range(0, tierWeapons.Count)]);
    }

    public void PurchaseWeapon(WeaponData weaponData)
    {
        if (weaponData != null)
        {
            // ������ ���� ��Ͽ� �߰�
            purchasedWeapons.Add(weaponData);

            // ù �������� ù ���Ű� �߻��� ���
            if (isFirstShop && !hasFirstPurchase)
            {
                hasFirstPurchase = true;
                // ������ ������� ������ ���� �������� ����
                foreach (var option in weaponOptions)
                {
                    if (option.WeaponData != weaponData)
                    {
                        option.WeaponData.price = option.WeaponData.currentTier switch
                        {
                            1 => option.WeaponData.tier1Price,
                            2 => option.WeaponData.tier2Price,
                            3 => option.WeaponData.tier3Price,
                            4 => option.WeaponData.tier4Price,
                            _ => option.WeaponData.tier1Price
                        };
                        option.UpdateUI(); // WeaponOptionUI�� UpdateUI �޼��� �ʿ�
                    }
                }
            }

            shopUI.SetActive(false);
            inventoryUI.SetActive(true);
            inventoryController.OnPurchaseItem(weaponData);
        }
    }

    public void RefreshShop()
    {
        if (playerStats.SpendCoins(currentRefreshCost))
        {
            currentRefreshCost += refreshCostIncrease;
            UpdateRefreshCostText();

            List<WeaponData> randomWeapons = GetRandomWeapons(weaponOptions.Length);
            for (int i = 0; i < weaponOptions.Length; i++)
            {
                if (i < randomWeapons.Count && weaponOptions[i] != null)
                {
                    weaponOptions[i].Initialize(randomWeapons[i], this);
                }
            }
        }
    }
}