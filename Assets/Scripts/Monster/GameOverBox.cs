using UnityEngine;

public class GameOverBox : MonoBehaviour
{
    [SerializeField] GameObject gameOverPanel;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "BodyMesh")
            GameOver();
    }
    private void Awake()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name == "GameOverPanel")
            {
                gameOverPanel = obj;
                break;
            }
        }
    }
    private void GameOver()
    {
        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);

        SoundManager.Instance.PlayGameOverBgm();
        // InGameManager�� ���ӿ��� �˸�
        InGameManager inGameManager = FindAnyObjectByType<InGameManager>();
        if (inGameManager != null)
            inGameManager.SetGameOver();
    }
}
