using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [Header("이동할 씬 설정")]
    [Tooltip("넘어갈 씬의 이름을 정확하게 입력하세요.")]
    public string targetSceneName;

    [Tooltip("이 태그를 가진 오브젝트와 충돌했을 때만 씬이 넘어갑니다.")]
    public string targetTag = "Player";

    private AudioSource adSfx;
    private AudioSource adBgm;

    // 일반적인 물리 충돌체에 부딪혔을 때 작동
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            LoadTargetScene();
        }
    }

    // IsTrigger가 체크된 트리거 충돌체에 부딪혔을 때 작동
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            LoadTargetScene();
        }
    }

    public void LoadTargetScene() // 튜토리얼 스킵버튼과 연결하기 위해 public으로 변경
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
            adSfx = SoundManager.Instance.sfxSource;
            adSfx.Stop();
            adBgm = SoundManager.Instance.bgmSource;
            adBgm.Stop();
        }
        else
            Debug.LogWarning("[SceneTransition] 이동할 씬의 이름이 설정되지 않았습니다!");
    }
}