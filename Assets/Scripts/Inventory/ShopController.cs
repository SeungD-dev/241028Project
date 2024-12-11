using UnityEngine;
using System.Collections.Generic;

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

        // Ƽ�� 1�� �ʱ�ȭ (���������� �׻� 1Ƽ��� ����)
        scaledWeapon.currentTier = 1;

        // �÷��̾� ������ ���� ���� �����ϸ�
        float levelScaling = 1f + ((playerStats.Level - 1) * 0.05f); // ������ 5% ����

        // ���� Ƽ���� ���� �����ϸ�
        TierStats currentTierStats = scaledWeapon.CurrentTierStats;
        currentTierStats.damage *= levelScaling;
        currentTierStats.projectileSpeed *= levelScaling;
        currentTierStats.attackDelay /= (1f + ((playerStats.Level - 1) * 0.02f)); // ������ 2% ������

        // ���� ����
        if (!isFirstShop)
        {
            int basePrice = originalWeapon.price;
            float priceScaling = 1f + ((playerStats.Level - 1) * 0.1f); // ������ 10% ����
            scaledWeapon.price = Mathf.RoundToInt(basePrice * priceScaling);
        }
        else
        {
            scaledWeapon.price = 0;
        }

        return scaledWeapon;
    }


    private List<WeaponData> GetRandomWeapons(int count)
    {
        List<WeaponData> allWeapons = new List<WeaponData>(weaponDatabase.weapons);
        List<WeaponData> randomWeapons = new List<WeaponData>();

        while (randomWeapons.Count < count && allWeapons.Count > 0)
        {
            int randomIndex = Random.Range(0, allWeapons.Count);
            randomWeapons.Add(allWeapons[randomIndex]);
            allWeapons.RemoveAt(randomIndex);
        }

        return randomWeapons;
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