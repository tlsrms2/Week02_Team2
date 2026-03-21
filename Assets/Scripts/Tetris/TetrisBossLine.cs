using UnityEngine;

public class TetrisBossLine : MonoBehaviour
{
    [Header("게임 오버 설정")]
    [Tooltip("플레이어가 보스와 닿았을 때 이동할 씬의 이름 (예: Title, Ending 등)")]
    public string gameOverSceneName = "Title"; 

    // Collider의 IsTrigger가 켜져 있을 때 감지
    private void OnTriggerEnter(Collider other)
    {
        CheckPlayerCollision(other.gameObject);
    }

    // Collider의 IsTrigger가 꺼져 있고 물리 충돌을 할 때 감지
    private void OnCollisionEnter(Collision collision)
    {
        CheckPlayerCollision(collision.gameObject);
    }

    private void CheckPlayerCollision(GameObject obj)
    {
        if (obj.CompareTag("Player"))
        {
            Debug.Log("[TetrisBoss] 플레이어와 닿았습니다! 게임 오버 처리 시작!");

            // SoundManager를 통해 게임 오버 브금 재생
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayGameOverBgm();
            }

            // GameManager를 통해 씬 이동(페이드 아웃 등 적용됨)
            if (GameManager.Instance != null && !string.IsNullOrEmpty(gameOverSceneName))
            {
                GameManager.Instance.LoadSceneByName(gameOverSceneName);
            }
        }
    }
}