using UnityEngine;

public class GameOverBox : MonoBehaviour
{
    [SerializeField] GameObject gameOverPanel;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "BodyMesh")
            GameOver();
    }
    private void GameOver()
    {
        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);

        // InGameManager에 게임오버 알림
        InGameManager inGameManager = FindAnyObjectByType<InGameManager>();
        if (inGameManager != null)
            inGameManager.SetGameOver();
    }
}
