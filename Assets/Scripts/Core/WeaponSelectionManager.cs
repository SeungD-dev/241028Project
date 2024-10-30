using System.Collections.Generic;
using UnityEngine;

public class WeaponSelectionManager : MonoBehaviour
{
    [SerializeField] private WeaponDatabase weaponDatabase;
    [SerializeField] private int selectionCount = 3;

    public List<WeaponData> SelectRandomWeapons()
    {
        List<WeaponData> availableWeapons = weaponDatabase.GetAllWeapons();
        List<WeaponData> selectedWeapons = new List<WeaponData>();

        foreach (var weapon in availableWeapons)
        {
            // ������ �⺻ ������� ���Ƽ ������ ����
            float finalDropRate = weapon.dropRate *
                weaponDatabase.GetRarityDropMultiplier(weapon.rarity);

            if (Random.Range(0, 100f) <= finalDropRate)
            {
                selectedWeapons.Add(weapon);
            }
        }

        // selectionCount(3��)���� ���� ���õ� ���, �������� ����
        while (selectedWeapons.Count > selectionCount && selectedWeapons.Count > 0)
        {
            selectedWeapons.RemoveAt(Random.Range(0, selectedWeapons.Count));
        }

        // selectionCount(3��)���� ���� ���õ� ���, �������� ä��
        while (selectedWeapons.Count < selectionCount && availableWeapons.Count > 0)
        {
            int randomIndex = Random.Range(0, availableWeapons.Count);
            if (!selectedWeapons.Contains(availableWeapons[randomIndex]))
            {
                selectedWeapons.Add(availableWeapons[randomIndex]);
            }
        }

        return selectedWeapons;
    }
}