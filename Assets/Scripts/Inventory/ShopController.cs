using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ShopController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponDatabase weaponDatabase;
    [SerializeField] private WeaponOptionUI[] weaponOptions;
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private GameObject shopUI;
    [SerializeField] private GameObject playerControlUI;
    [SerializeField] private GameObject playerStatsUI;

    private bool isFirstShop = true;
    private PlayerStats playerStats;

    public void InitializeShop()
    {
        // PlayerStats ���� ��������
        if (playerStats == null)
        {
            playerStats = GameManager.Instance.PlayerStats;
            if (playerStats == null)
            {
                Debug.LogError("PlayerStats reference is null in ShopController!");
                return;
            }
        }

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
                // ���� ���� �����͸� �����Ͽ� ������ �°� ����
                WeaponData scaledWeapon = ScaleWeaponToPlayerLevel(randomWeapons[i]);

                // ù �����̸� ���� ������ 0���� ����
                if (isFirstShop)
                {
                    scaledWeapon.price = 0;
                }

                weaponOptions[i].Initialize(scaledWeapon, this);
            }
        }

        isFirstShop = false;
    }

    private WeaponData ScaleWeaponToPlayerLevel(WeaponData originalWeapon)
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats is null when trying to scale weapon!");
            return originalWeapon;
        }

        // ���� �����͸� �������� �ʱ� ���� ����
        WeaponData scaledWeapon = ScriptableObject.Instantiate(originalWeapon);

        // ���� �����ϸ�
        float levelScaling = 1f + ((playerStats.Level - 1) * 0.05f);
        TierStats currentTierStats = scaledWeapon.CurrentTierStats;
        currentTierStats.damage *= levelScaling;
        currentTierStats.projectileSpeed *= levelScaling;
        currentTierStats.attackDelay /= (1f + ((playerStats.Level - 1) * 0.02f));

        // ù �����̸� ������ ������ 0���� ����
        if (isFirstShop)
        {
            scaledWeapon.price = 0;
        }
        else
        {
            // �Ϲ� ������ ��� ������ ���� ���� �����ϸ� ����
            float priceScaling = 1f + ((playerStats.Level - 1) * 0.1f);
            int basePrice = scaledWeapon.currentTier switch
            {
                1 => scaledWeapon.tier1Price,
                2 => scaledWeapon.tier2Price,
                3 => scaledWeapon.tier3Price,
                4 => scaledWeapon.tier4Price,
                _ => scaledWeapon.tier1Price
            };
            scaledWeapon.price = Mathf.RoundToInt(basePrice * priceScaling);
        }

        return scaledWeapon;
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
        // ���� ���������� �� Ƽ�� Ȯ���� �ѹ��� ��������
        float[] tierProbs = weaponDatabase.tierProbability.GetTierProbabilities(playerStats.Level);

        // ���� ������ Ƽ�� ���� (0-100 ������ ��)
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

        // ���õ� Ƽ���� ����� �߿��� ���� ����
        List<WeaponData> tierWeapons = weaponDatabase.weapons
            .Where(w => w.currentTier == selectedTier)
            .ToList();

        if (tierWeapons.Count == 0)
        {
            Debug.LogWarning($"No weapons found for tier {selectedTier}");
            return null;
        }

        // ���⸦ �����Ͽ� ��ȯ (���� ������ ScaleWeaponToPlayerLevel���� ó��)
        return ScriptableObject.Instantiate(tierWeapons[Random.Range(0, tierWeapons.Count)]);
    }
    public void PurchaseWeapon(WeaponData weaponData)
    {
        if (weaponData != null)
        {
            shopUI.SetActive(false);
            inventoryUI.SetActive(true);
            inventoryController.OnPurchaseItem(weaponData);
        }
    }

    public void RefreshShop()
    {
        InitializeShop();
    }
}