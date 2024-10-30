using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Inventory/Weapon Database")]
public class WeaponDatabase : ScriptableObject
{
    [SerializeField] private List<WeaponData> weapons;

    [Header("Rarity Settings")]
    [SerializeField] private SerializableDictionary<WeaponRarity, Color> rarityColors;
    [SerializeField] private SerializableDictionary<WeaponRarity, float> rarityDropMultipliers;

    // �⺻ ���� ��ȸ
    public WeaponData GetWeaponById(string id)
    {
        return weapons.Find(w => w.id == id);
    }

    // Ư�� ���Ƽ�� ���� ��� ��ȸ
    public List<WeaponData> GetWeaponsByRarity(WeaponRarity rarity)
    {
        return weapons.FindAll(w => w.rarity == rarity);
    }

    // ���Ƽ�� ���� ��ȸ
    public Color GetRarityColor(WeaponRarity rarity)
    {
        return rarityColors[rarity];
    }

    // ���Ƽ�� ��� ���� ��ȸ
    public float GetRarityDropMultiplier(WeaponRarity rarity)
    {
        return rarityDropMultipliers[rarity];
    }

    // ��ü ���� ��� ��ȸ
    public List<WeaponData> GetAllWeapons()
    {
        return new List<WeaponData>(weapons);
    }
}
