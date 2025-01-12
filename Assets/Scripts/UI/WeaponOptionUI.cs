using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponOptionUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image weaponImage;
    [SerializeField] private TextMeshProUGUI weaponLevelText;
    [SerializeField] private Button myPurchaseButton;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI weaponTierText;

    private ShopController shopUI;
    private PlayerStats playerStats;
    private WeaponData weaponData;
    private bool isPurchased = false;

    public WeaponData WeaponData => weaponData;

    private void Start()
    {
        playerStats = GameManager.Instance.PlayerStats;
    }

    public void Initialize(WeaponData weapon, ShopController shop)
    {
        weaponData = weapon;
        shopUI = shop;
        isPurchased = false;
        SetupUI();
        SetupButtons();
        UpdatePurchaseButtonState();
    }

    private void SetupUI()
    {
        if (weaponData == null) return;

        weaponNameText.text = weaponData.weaponName;
        weaponImage.sprite = weaponData.weaponIcon;
        descriptionText.text = weaponData.weaponDescription;

        Color tierColor = weaponData.GetTierColor();
        weaponImage.color = tierColor;
        weaponImage.preserveAspect = true;

        priceText.text = weaponData.price == 0 ? "FREE" : $"{weaponData.price} Coins";

        if (weaponTierText != null)
        {
            weaponTierText.text = $"Tier {weaponData.currentTier}";
            weaponTierText.color = tierColor;
        }
    }

    public void ResetPurchaseState()
    {
        isPurchased = false;
        UpdatePurchaseButtonState();
        SetupUI();  // UI ��ü�� ����
    }

    private void SetupButtons()
    {
        if (myPurchaseButton != null)
        {
            myPurchaseButton.onClick.RemoveAllListeners();
            myPurchaseButton.onClick.AddListener(OnPurchaseClicked);
        }
    }

    private void UpdatePurchaseButtonState()
    {
        if (myPurchaseButton != null && playerStats != null && weaponData != null)
        {
            bool canAfford = (weaponData.price == 0 || playerStats.CoinCount >= weaponData.price);
            bool canPurchase = canAfford && !isPurchased;

            myPurchaseButton.interactable = canPurchase;

            // ��ư ���� ����
            myPurchaseButton.GetComponent<Image>().color = canPurchase ? Color.white : Color.gray;

            // ���� �ؽ�Ʈ ������Ʈ
            if (priceText != null)
            {
                if (isPurchased)
                {
                    priceText.text = "SOLD";
                }
                else
                {
                    priceText.text = weaponData.price == 0 ? "FREE" : $"{weaponData.price} Coins";
                }
            }
        }
    }
    private void OnPurchaseClicked()
    {
        if (weaponData == null || shopUI == null || playerStats == null || isPurchased) return;

        // ���� ������ ShopController�� ����
        shopUI.OnPurchaseClicked(this);
    }

    public void SetPurchased(bool purchased)
    {
        isPurchased = purchased;
        UpdatePurchaseButtonState();
    }

    public void UpdateUI()
    {
        SetupUI();
        UpdatePurchaseButtonState();
    }

    private void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.OnCoinChanged += OnCoinCountChanged;
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.OnCoinChanged -= OnCoinCountChanged;
        }
    }

    private void OnCoinCountChanged(int newCoinCount)
    {
        UpdatePurchaseButtonState();
    }
}