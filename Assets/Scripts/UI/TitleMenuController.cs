using UnityEngine;
using UnityEngine.UI;

public class TitleMenuController : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;

    [Header("레트로 깜빡임 연출")]
    [Tooltip("주기적으로 깜빡일 텍스트 오브젝트 (INSERT COIN 등)")]
    [SerializeField] private GameObject blinkText;
    [Tooltip("깜빡이는 간격 (초)")]
    [SerializeField] private float blinkInterval = 0.5f;

    private float blinkTimer;

    private void Start()
    {
        // 에디터에서 일일이 OnClick에 넣을 필요 없이 코드로 자동 연결합니다.
        if (startButton != null) startButton.onClick.AddListener(OnGameStart);
        if (exitButton != null) exitButton.onClick.AddListener(OnGameExit);
    }

    private void Update()
    {
        // 텍스트 깜빡임(Blink) 로직
        if (blinkText != null)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkInterval)
            {
                // activeSelf 상태를 반전시켜 1프레임만에 즉시 껐다 켰다를 반복합니다.
                blinkText.SetActive(!blinkText.activeSelf);
                blinkTimer = 0f; // 타이머 초기화
            }
        }
    }

    private void OnGameStart()
    {
        Debug.Log("게임 시작 클릭됨");
        
        // 기존에 구현되어 있는 GameManager를 활용하여 씬을 전환합니다.
        if (GameManager.Instance != null)
        {
            Debug.LogError("게임 시작 클릭됨");
        }
        else
        {
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다!");
        }
    }

    private void OnGameExit()
    {
        Debug.Log("게임 종료 클릭됨");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}