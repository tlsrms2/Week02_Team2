using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // 씬 이름을 상수로 관리하여 오타를 방지합니다.
    public const string SceneTitle = "TitleScene";
    public const string SceneIntro = "IntroScene";
    public const string SceneStage1 = "Stage01_PackMan";
    public const string SceneStage2 = "Stage02_DongKingKong";
    public const string SceneStage3 = "Stage03_Tetris";

    private void Awake()
    {
        // 싱글톤 패턴: 게임 매니저가 오직 한 개만 존재하도록 보장하고, 씬이 넘어가도 유지합니다.
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 빌드 세팅(Build Settings)에 등록된 순서대로 다음 씬을 로드합니다.
    public void LoadNextScene()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("마지막 씬입니다. 타이틀로 돌아갑니다.");
            LoadSceneByName(SceneTitle);
        }
    }

    // 특정 씬 이름으로 바로 넘어가는 기능 (예: GameManager.Instance.LoadSceneByName(GameManager.SceneStage1))
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}