using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using System.Collections;

public class ShopController : MonoBehaviour
{
    [Header("Refresh Settings")]
    [SerializeField] private int initialRefreshCost = 5;
    [SerializeField] private int refreshCostIncrease = 1;
    [SerializeField] private Button refreshButton;
    private int currentRefreshCost;

    [Header("References")]
    [SerializeField] private WeaponDatabase weaponDatabase;
    [SerializeField] private WeaponOptionUI[] weaponOptions;
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private GameObject shopUI;
    [SerializeField] private GameObject playerControlUI;
    [SerializeField] private GameObject playerStatsUI;
    [SerializeField] private GameObject noticeUI;
    [SerializeField] private ItemGrid mainInventoryGrid;
    [SerializeField] private GameObject weaponPrefab;
    [Header("UI Texts")]
    [SerializeField] private TMPro.TextMeshProUGUI refreshCostText;
    [SerializeField] private TMPro.TextMeshProUGUI playerCoinsText;
    [SerializeField] private TMPro.TextMeshProUGUI noticeText;
    [SerializeField] private float noticeDisplayTime = 2f;
    [Header("Transition Effect")]
    [SerializeField] private ScreenTransitionEffect transitionEffect;
    public bool isFirstShop = true;
    private bool hasFirstPurchase = false;
    private bool isNoticeClosed = true;
    private PlayerStats playerStats;
    private HashSet<WeaponData> purchasedWeapons = new HashSet<WeaponData>();
    private Coroutine currentNoticeCoroutine;
    private void Start()
    {
        // �������� ��ư �̺�Ʈ ����
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshShop);
        }
        else
        {
            Debug.LogError("Refresh button reference is missing!");
        }
        if (noticeUI != null)
        {
            noticeUI.SetActive(false);
        }
        if (mainInventoryGrid != null)
        {
            mainInventoryGrid.ForceInitialize();
        }
    }
    private void OnDisable()
    {
        // ������Ʈ�� ��Ȱ��ȭ�� �� ���� ���� �ڷ�ƾ ����
        if (currentNoticeCoroutine != null)
        {
            StopCoroutine(currentNoticeCoroutine);
            currentNoticeCoroutine = null;
        }

        // Notice UI�� Ȱ��ȭ�� ���·� �������� �ʵ��� ����
        if (noticeUI != null)
        {
            noticeUI.SetActive(false);
        }
    }
    private void OnDestroy()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(RefreshShop);
        }

        if (playerStats != null)
        {
            playerStats.OnCoinChanged -= UpdatePlayerCoinsText;
        }
        StopAllCoroutines();
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
        if (inventoryController != null)
        {
            // ���� UI ��Ȱ��ȭ
            shopUI.SetActive(false);

            // �κ��丮 ��Ʈ�ѷ��� ���� �κ��丮 ����
            inventoryController.OpenInventory();
        }
        else
        {
            Debug.LogError("InventoryController reference is missing!");
        }
    }

    public void OpenShop()
    {
        if (hasFirstPurchase)
        {
            isFirstShop = false;
        }

        // ���� UI �غ� (���� ǥ�� �� ��)
        PrepareShop(false);

        if (transitionEffect != null)
        {
            // �ȿ��� �ٱ����� ȿ�� (reverseEffect = true)
            transitionEffect.reverseEffect = true;
            transitionEffect.PlayTransition(() => {
                ShowShopUI();
            });
        }
        else
        {
            ShowShopUI();
        }
    }

    private void PrepareShop(bool showUI)
    {
        playerControlUI.SetActive(false);
        playerStatsUI.SetActive(false);

        if (showUI)
        {
            shopUI.SetActive(true);
            inventoryUI.SetActive(false);
        }

        // ù ������ �ƴ� ��� ���ο� ���� �ɼ��� ����
        if (!isFirstShop)
        {
            InitializeNewWeapons();
        }
        // ù �����̸鼭 ���� �������� ���� ��� ���� ���� ǥ��
        else if (isFirstShop && !hasFirstPurchase)
        {
            InitializeFreeWeapons();
        }
        // ù �������� �̹� ���������� ���� ù ������ ���
        // ���� ���̺�� �������� �ʰ� �������� ���ƿ� ��쿡 �ش�
        else if (isFirstShop && hasFirstPurchase)
        {
            foreach (var option in weaponOptions)
            {
                if (option != null)
                {
                    option.SetPurchased(true);
                }
            }
        }

        UpdateRefreshCostText();
    }
    private void ShowShopUI()
    {
        shopUI.SetActive(true);
        inventoryUI.SetActive(false);

        // ���� ���°� �Ͻ����� ���°� �ƴ϶�� �Ͻ������� ����
        if (GameManager.Instance.currentGameState != GameState.Paused)
        {
            GameManager.Instance.SetGameState(GameState.Paused);
        }
    }
    private void InitializeNewWeapons()
    {
        List<WeaponData> randomWeapons = GetRandomWeapons(weaponOptions.Length);

        foreach (var option in weaponOptions)
        {
            if (option != null)
            {
                option.ResetPurchaseState();
            }
        }

        for (int i = 0; i < weaponOptions.Length; i++)
        {
            if (i < randomWeapons.Count && weaponOptions[i] != null)
            {
                weaponOptions[i].Initialize(randomWeapons[i], this);
            }
        }
    }
    public void OnPurchaseClicked(WeaponOptionUI weaponOption)
    {
        if (weaponOption == null || weaponOption.WeaponData == null || playerStats == null) return;

        WeaponData weaponData = weaponOption.WeaponData;

        // �κ��丮 ���� üũ
        if (!HasEnoughSpaceForItem(weaponData))
        {
            ShowNotice("Not enough space in inventory!");
            return;
        }

        // ���� ����
        if (weaponData.price == 0 || playerStats.SpendCoins(weaponData.price))
        {
            SoundManager.Instance?.PlaySound("Button_sfx", 1f, false);
            weaponOption.SetPurchased(true);
            PurchaseWeapon(weaponData);
        }
    }
    private bool HasEnoughSpaceForItem(WeaponData weaponData)
    {
        if (mainInventoryGrid != null && !mainInventoryGrid.IsInitialized)
        {
            mainInventoryGrid.ForceInitialize();
        }
        // weaponData üũ
        if (weaponData == null)
        {
         return false;
        }

        // mainInventoryGrid üũ
        if (mainInventoryGrid == null)
        {            
            return false;
        }

        // weaponPrefab üũ
        if (weaponPrefab == null)
        {
            return false;
        }

        try
        { 
            // �ӽ� InventoryItem ����
            GameObject tempObj = Instantiate(weaponPrefab);
            if (tempObj == null)
            {
                Debug.LogError("Failed to instantiate weapon prefab!");
                return false;
            }

            InventoryItem tempItem = tempObj.GetComponent<InventoryItem>();
            if (tempItem == null)
            {
                Destroy(tempObj);
                return false;
            }
            tempItem.Initialize(weaponData);
            // ���� üũ
            Vector2Int? freePosition = mainInventoryGrid.FindSpaceForObject(tempItem);
            // �ӽ� ������Ʈ ����
            Destroy(tempObj);

            return freePosition.HasValue;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in HasEnoughSpaceForItem: {e.Message}\nStackTrace: {e.StackTrace}");
            return false;
        }
    }
    private void ShowNotice(string message)
    {
        if (noticeUI == null || noticeText == null) return;

        // ������ ���� ���� �ڷ�ƾ�� �ִٸ� ����
        if (currentNoticeCoroutine != null)
        {
            StopCoroutine(currentNoticeCoroutine);
        }

        noticeText.text = message;
        noticeUI.SetActive(true);

        currentNoticeCoroutine = StartCoroutine(HideNoticeAfterDelay());
    }


    private IEnumerator HideNoticeAfterDelay()
    {
        yield return new WaitForSecondsRealtime(noticeDisplayTime);

        if (noticeUI != null)
        {
            noticeUI.SetActive(false);
        }
        currentNoticeCoroutine = null;
    }
    private void InitializeFreeWeapons()
    {
        List<WeaponData> randomWeapons = GetRandomWeapons(weaponOptions.Length);

        foreach (var option in weaponOptions)
        {
            if (option != null)
            {
                option.ResetPurchaseState();
            }
        }

        for (int i = 0; i < weaponOptions.Length; i++)
        {
            if (i < randomWeapons.Count && weaponOptions[i] != null)
            {
                WeaponData weaponCopy = ScriptableObject.Instantiate(randomWeapons[i]);
                randomWeapons[i].price = 0;
                weaponOptions[i].Initialize(randomWeapons[i], this);
            }
        }
    }
    public WeaponOptionUI[] GetWeaponOptions()
    {
        return weaponOptions;
    }
    public void InitializeShop()
    {
        if (GameManager.Instance.currentGameState != GameState.Paused)
        {
            GameManager.Instance.SetGameState(GameState.Paused);
        }

        // ������ ���� ���� ������ �������� ��� �ʱ�ȭ
        currentRefreshCost = initialRefreshCost;
        UpdateRefreshCostText();

        // PlayerStats ���� ����
        if (playerStats == null)
        {
            playerStats = GameManager.Instance.PlayerStats;
            if (playerStats == null)
            {
                Debug.LogError("PlayerStats reference is null in ShopController!");
                return;
            }
            playerStats.OnCoinChanged += UpdatePlayerCoinsText;
        }

        // UI �ʱ�ȭ
        UpdatePlayerCoinsText(playerStats.CoinCount);
        playerControlUI.SetActive(false);
        playerStatsUI.SetActive(false);
        shopUI.SetActive(true);

        // ���� �ɼ� �ʱ�ȭ
        List<WeaponData> randomWeapons = GetRandomWeapons(weaponOptions.Length);

        // ��� ���� �ɼ� �ʱ�ȭ
        for (int i = 0; i < weaponOptions.Length; i++)
        {
            if (i < randomWeapons.Count && weaponOptions[i] != null)
            {
                WeaponData weapon = randomWeapons[i];
                // ù �����̰� ���� �������� �ʾҴٸ� ����� ����
                if (isFirstShop && !hasFirstPurchase)
                {
                    weapon.price = 0;
                }

                // ���� �ɼ� �ʱ�ȭ (�׻� ���� ������ ���·� ����)
                weaponOptions[i].ResetPurchaseState();
                weaponOptions[i].Initialize(weapon, this);
            }
        }
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
    private void GenerateNewWeaponOptions()
    {
        List<WeaponData> randomWeapons = GetRandomWeapons(weaponOptions.Length);

        for (int i = 0; i < weaponOptions.Length; i++)
        {
            if (i < randomWeapons.Count && weaponOptions[i] != null)
            {
                weaponOptions[i].ResetPurchaseState();
                weaponOptions[i].Initialize(randomWeapons[i], this);
            }
        }
    }
    public void PurchaseWeapon(WeaponData weaponData)
    {
        if (weaponData != null)
        {
            // ������ ���� ��Ͽ� �߰�
            purchasedWeapons.Add(weaponData);

            // ù ���������� ù ���� ó��
            if (isFirstShop && !hasFirstPurchase)
            {
                hasFirstPurchase = true;
                // ù ���������� ������ ������� ��Ȱ��ȭ
                foreach (var option in weaponOptions)
                {
                    if (option.WeaponData != weaponData)
                    {
                        option.SetPurchased(true);
                    }
                }
            }

            shopUI.SetActive(false);
            inventoryUI.SetActive(true);
            inventoryController.OnPurchaseItem(weaponData);
        }
    }
  

    public void CloseShop()
    {
        shopUI.SetActive(false);
    }
    public void RefreshShop()
    {
        if (playerStats == null || !playerStats.SpendCoins(currentRefreshCost)) return;

        // �������� ��� ����
        currentRefreshCost += refreshCostIncrease;
        UpdateRefreshCostText();

        // ���ο� ���� ��� ����
        GenerateNewWeaponOptions();

        // ȿ���� ���
        SoundManager.Instance?.PlaySound("Button_sfx", 1f, false);
    }
}