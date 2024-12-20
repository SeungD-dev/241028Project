using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Weapon")]
public class WeaponData : ScriptableObject
{
    public int width = 1;
    public int height = 1;

    public Sprite weaponIcon;
    public WeaponRarity rarity;
    public WeaponType weaponType;
    public int price;
    public string weaponName;
    public string weaponDescription;
   

    [Header("Combat Stats")]
    public float projectileSpeed = 10f;
    public float weaponLevel;
    public int weaponDamage;
    public float attackDelay;

    [Header("Prefabs")]
    public GameObject projectilePrefab;

   
}


