using System.Collections.Generic;
using UnityEngine;

public class WeaponSelectionManager : MonoBehaviour
{
    //[SerializeField] private WeaponDatabase weaponDatabase;
    //[SerializeField] private int selectionCount = 3;

    //// ���Ƽ�� ���� Ȯ��
    //private readonly Dictionary<WeaponRarity, float> rarityDropRates = new Dictionary<WeaponRarity, float>()
    //{
    //    { WeaponRarity.common, 75f },
    //    { WeaponRarity.uncommon, 15f },
    //    { WeaponRarity.magic, 8f },
    //    { WeaponRarity.epic, 2f }
    //};

    //public List<WeaponData> SelectRandomWeapons()
    //{
    //    List<WeaponData> availableWeapons = weaponDatabase.GetAllWeapons();
    //    List<WeaponData> selectedWeapons = new List<WeaponData>();

    //    // selectionCount(3��)��ŭ ���� ����
    //    while (selectedWeapons.Count < selectionCount)
    //    {
    //        // Ȯ���� ���� ���Ƽ ����
    //        float randomValue = Random.Range(0f, 100f);
    //        WeaponRarity selectedRarity = WeaponRarity.common;
    //        float cumulative = 0f;

    //        foreach (var rateEntry in rarityDropRates)
    //        {
    //            cumulative += rateEntry.Value;
    //            if (randomValue <= cumulative)
    //            {
    //                selectedRarity = rateEntry.Key;
    //                break;
    //            }
    //        }

    //        // ���õ� ���Ƽ�� ����� �߿��� ���� ����
    //        List<WeaponData> weaponsOfRarity = weaponDatabase.GetWeaponsByRarity(selectedRarity);
    //        if (weaponsOfRarity.Count > 0)
    //        {
    //            WeaponData selectedWeapon;
    //            do
    //            {
    //                selectedWeapon = weaponsOfRarity[Random.Range(0, weaponsOfRarity.Count)];
    //            }
    //            while (selectedWeapons.Contains(selectedWeapon)); // �ߺ� ����

    //            selectedWeapons.Add(selectedWeapon);
    //        }
    //    }

    //    return selectedWeapons;
    //}

    //// ���Ƽ�� ��ӷ� ������ ���� public �޼��� (�ʿ��� ���)
    //public void UpdateDropRate(WeaponRarity rarity, float newRate)
    //{
    //    if (rarityDropRates.ContainsKey(rarity))
    //    {
    //        rarityDropRates[rarity] = newRate;
    //    }
    //}

    //// ���� ��ӷ� Ȯ���� ���� public �޼��� (�ʿ��� ���)
    //public float GetDropRate(WeaponRarity rarity)
    //{
    //    return rarityDropRates.ContainsKey(rarity) ? rarityDropRates[rarity] : 0f;
    //}
}