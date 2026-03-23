using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameMenuManager: MonoBehaviour
{
    public static InGameMenuManager Instance;

    public enum InputDeviceType
    {
        KeyboardMouse,
        Gamepad
    }

    [Header("카메라 페이드")]
    [SerializeField] private Image blackOutImage;
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("타이틀 참조")]
    [SerializeField] private Image titlePanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TitleMenuController titleMenu;
    public float typingSpeed = 0.05f; // 글자 타이핑 속도
    public float lineDelay = 0.3f;   // 줄 간격 딜레이

    [Header("타이핑 사운드 설정")]
    [SerializeField] private AudioSource typeAudioSource;
    [SerializeField] private AudioClip typeSound;
    [SerializeField][Range(0f, 0.5f)] private float pitchRange = 0.1f;

    [Header("타이틀 사용")]
    [SerializeField] private Material glitchMaterial;
    [SerializeField] private Material potMaterial;
    [SerializeField] private Material twistMaterial;
    [SerializeField] private TMP_FontAsset StartFont;
    [SerializeField] private TMP_FontAsset LodingFont;
    [Header("조작 설정")]
    // 게임 전체에서 공유될 현재 조작 기기 상태
    public InputDeviceType currentInputDevice = InputDeviceType.KeyboardMouse;
    private Coroutine blink;

    private void Start()
    {
        titlePanel.material.SetFloat("_GlitchIntensity", 0f);
        titleText.font = LodingFont;
        titleText.text = "";
        titleText.alignment = TextAlignmentOptions.TopLeft;
        titleText.fontSize = 50;
        titleText.color = Color.green;
        StartCoroutine(Glitch_Title());
    }

    private void Awake()
    {
        // 싱글톤 패턴: 게임 매니저가 오직 한 개만 존재하도록 보장하고, 씬이 넘어가도 유지합니다.
        //if (Instance == null) Instance = this;
        //else Destroy(gameObject);
    }
    public void ExitButton()
    {
        Application.Quit();
    }

    // 특정 씬 이름으로 바로 넘어가는 기능 (예: GameManager.Instance.LoadSceneByName(GameManager.SceneStage1))
    public void LoadSceneByName(string sceneName)
    {
        titleMenu.OutMenu();
        titleText.enabled = false;
        StartCoroutine(StartGame(sceneName, 1f));
    }
    // 게임 시작 버튼
    private IEnumerator StartGame(string name, float duration)
    {
        titlePanel.enabled = true;
        float elapsed = 0f;

        titlePanel.material = potMaterial;
        titlePanel.material.SetFloat("_Reveal", 0f);

        // EmissionIntensity 0 → 1
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            titlePanel.material.SetFloat("_Reveal", Mathf.Lerp(0f, 1f, elapsed / duration));
            yield return null;
        }
        titlePanel.material.SetFloat("_Reveal", 1f);

        yield return new WaitForSeconds(0.3f);
        titlePanel.material = null;

        yield return StartCoroutine(TwistEffect());
    }

    // 인게임 전 마지막 플레이 영상
    private IEnumerator TwistEffect()
    {
        float twistDuration = 1.5f;

        titlePanel.enabled = true;
        titlePanel.material = twistMaterial;
        titlePanel.material.SetFloat("_SwitrlStrength", 0f);

        // 0 → 50
        float elapsed = 0f;
        while (elapsed < twistDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / twistDuration;
            titlePanel.material.SetFloat("_SwitrlStrength", Mathf.Lerp(0f, 50f, t));
            yield return null;
        }

        titlePanel.material.SetFloat("_SwitrlStrength", 50f);

        // 블랙아웃 페이드 아웃
        yield return StartCoroutine(BlackOutFade());

        yield return new WaitForSeconds(0.3f);

        StopAllCoroutines();
        SceneManager.LoadScene("Stage03_Tetris");
    }

    private IEnumerator BlackOutFade()
    {
        // 활성화 + 알파 0으로 시작
        blackOutImage.gameObject.SetActive(true);

        Color color = Color.black;
        color.a = 0f;
        blackOutImage.color = color;

        // 알파 0 → 1
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            blackOutImage.color = color;
            yield return null;
        }

        color.a = 1f;
        blackOutImage.color = color;
    }

    // 게임 시작 할때 실행됨.
    private IEnumerator Glitch_Title()
    {
        yield return StartCoroutine(FadeOut(1f));

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(FadeIn(2f));
    }
    private IEnumerator FadeIn(float duration)
    {
        float elapsed = 0f;
        Color color = Color.white;
        titleText.text = "소년은 끝나지 않는다\n\n\n\n\n";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 68.3f;
        titleText.font = StartFont;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsed / duration);
            titleText.color = color;
            yield return null;
        }

        color.a = 1f;
        titleText.color = color;

        titleMenu.InitMenu();
    }

    // 페이드 아웃 (1 → 0)
    private IEnumerator FadeOut(float duration)
    {
        SoundManager.Instance.PlayTitleBgm();

        float elapsed = 0f;
        Color color = titleText.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsed / duration);
            titleText.color = color;
            yield return null;
        }

        color.a = 0f;
        titleText.color = color;
    }
}