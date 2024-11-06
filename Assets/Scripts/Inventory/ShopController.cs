using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class ShopController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponDatabase weaponDatabase;
    [SerializeField] private WeaponOptionUI[] weaponOptions; // 3���� WeaponOption ����
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private InventoryController inventoryController;

    private void Start()
    {
        InitializeShop();
    }

    public void InitializeShop()
    {
        gameObject.SetActive(true);
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
                weaponOptions[i].Initialize(randomWeapons[i], this);
            }
        }
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
            // ���� UI �ݱ�
            gameObject.SetActive(false);

            // �κ��丮 UI ����
            inventoryUI.SetActive(true);

            // ������ ���⸦ �κ��丮�� �߰�
            inventoryController.CreatePurchasedItem(weaponData);
        }
    }

    public void SellWeapon(WeaponData weaponData)
    {
        if (weaponData != null)
        {
            Debug.Log($"Selling weapon: {weaponData.weaponName}");
            // �Ǹ� ���� ����
        }
    }

    public void RefreshShop()
    {
        InitializeShop();
    }
}
