using Unity.Cinemachine;
using UnityEngine;

public class CombatSceneInitializer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private ShopController shopController;
    [SerializeField] private CombatController combatController;
    [SerializeField] private GameOverController gameOverController;
    [SerializeField] private WaveManager waveManager;
    private void Start()
    {
        // �� �ε� �� �÷��̾� ��ġ ����
        if (MapManager.Instance != null)
        {
            GameMap map = MapManager.Instance.LoadMap();
            if (map != null && playerStats != null)
            {
                // �÷��̾ ������ ��ġ
                Vector2 startPosition = MapManager.Instance.GetPlayerStartPosition();
                playerStats.transform.position = startPosition;            
            }
        }

        // ���� �Ŵ��� ���� ����
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCombatSceneReferences(
                playerStats,
                shopController,
                combatController,
                gameOverController
            );

            // WaveManager �ʱ�ȭ
            if (waveManager != null)
            {
                waveManager.EnsureInitialized(MapManager.Instance.CurrentMap);
            }

            // �ʱ� �Ͻ����� ���� ����
            GameManager.Instance.SetGameState(GameState.Paused);

            // ù ���� ����
            OpenInitialShopPhase();
        }
    }

    private void OpenInitialShopPhase()
    {
        if (shopController != null)
        {
            // ù ���� ǥ��
            shopController.isFirstShop = true;
            shopController.InitializeShop();
        }
    }
}