using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // 씬 이름을 상수로 관리하여 오타를 방지합니다.
    public const string SceneTitle = "TitleScene";
    public const string SceneIntro = "IntroScene";
    public const string SceneStage1 = "Stage01_PackMan";
    public const string SceneStage2 = "Stage02_DongKingKong";
    public const string SceneStage3 = "Stage03_Tetris";

    [Header("타이틀 참조")]
    [SerializeField] private Image titlePanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TitleMenuController titleMenu;
    public float typingSpeed = 0.05f; // 글자 타이핑 속도
    public float lineDelay = 0.3f;   // 줄 간격 딜레이
    [Header("타이틀 사용")]
    [SerializeField] private Material glitchMaterial;
    [SerializeField] private TMP_FontAsset StartFont;
    [SerializeField] private TMP_FontAsset LodingFont;
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
    public void GuideButton()
    {
        Debug.Log("가이드 아직 없음");
    }
    public void ExitButton()
    {
        Application.Quit();
    }

    // 특정 씬 이름으로 바로 넘어가는 기능 (예: GameManager.Instance.LoadSceneByName(GameManager.SceneStage1))
    public void LoadSceneByName(string sceneName)
    {
        titleMenu.OutMenu();
        StartCoroutine(StartGame(sceneName));
    }
    private IEnumerator StartGame(string name)
    {
        titleText.font = LodingFont;
        titleText.text = "";
        titleText.alignment = TextAlignmentOptions.TopLeft;
        titleText.fontSize = 50;
        titleText.color = Color.green;
        yield return new WaitForSeconds(0.5f);

        // 1번째 줄 - 8비트 시스템 초기화
        yield return StartCoroutine(DotBlinkLine("8-BIT SYSTEM INITIALIZING"));

        // 2번째 줄 - ROM 체크 → OK 연출
        yield return StartCoroutine(TypeLine("CHECKING INTERNAL ROM... "));
        yield return new WaitForSeconds(0.6f);
        yield return StartCoroutine(TypeLine("OK"));
        titleText.text += "\n";
        yield return new WaitForSeconds(lineDelay);

        // 3번째 줄 - 배터리 한 칸씩 채워짐
        yield return StartCoroutine(BatteryLine());

        // 4번째 줄 - 카트리지 슬롯 읽기
        yield return StartCoroutine(DotBlinkLine("READING CARTRIDGE SLOT"));

        // 5번째 줄 - 링크 케이블
        yield return StartCoroutine(TypeLine("LINK CABLE: NOT CONNECTED"));
        titleText.text += "\n";
        yield return new WaitForSeconds(lineDelay);

        // 6번째 줄 - 스프라이트 데이터 로딩 퍼센트
        yield return StartCoroutine(LoadingLine());

        // 7번째 줄 - 픽셀 엔진 준비 완료
        yield return StartCoroutine(TypeLine("PIXEL ENGINE READY."));
        titleText.text += "\n";
        yield return new WaitForSeconds(lineDelay);

        // 빈 줄
        titleText.text += "\n";

        // 마지막 줄
        yield return StartCoroutine(TypeLine("PLEASE WAIT A SECOND..."));

        // 커서 깜빡임
        blink = StartCoroutine(BlinkCursor());
        yield return new WaitForSeconds(2f);
        StopCoroutine(blink);
        titleText.text = titleText.text.TrimEnd('_'); // 혹시 _ 남아있으면 제거
        yield return StartCoroutine(FadeOut(1f));

        // 글리치 효과 시작
        titlePanel.material = glitchMaterial;
        titlePanel.material.SetFloat("_GlitchIntensity", 0f);

        float intensity = 0f;
        while (intensity < 1f)
        {
            intensity = Mathf.MoveTowards(intensity, 1f, 0.3f * Time.deltaTime);
            titlePanel.material.SetFloat("_GlitchIntensity", intensity);
            yield return null;
        }

        titlePanel.material.SetFloat("_GlitchIntensity", 1f);

        yield return new WaitForSeconds(0.5f);

        // 씬 전환
        SceneManager.LoadScene(name);
    }


    // 게임 시작 할때 실행됨.
    private IEnumerator Glitch_Title()
    {
        // 1번째 줄 - 일반 타이핑
        yield return StartCoroutine(TypeLine("SYSTEM BOOT GameLab A2-Team.Unity...\n"));

        // 2번째 줄 - CHECKING → OK 연출
        yield return StartCoroutine(TypeLine("CHECKING INTERNAL RAM... "));
        yield return new WaitForSeconds(0.6f);
        yield return StartCoroutine(TypeLine("OK"));
        titleText.text += "\n";
        yield return new WaitForSeconds(lineDelay);

        // 3번째 줄 - 배터리 한 칸씩 채워짐
        yield return StartCoroutine(BatteryLine());

        // 4번째 줄 - 점 깜빡이며 카트리지 삽입
        yield return StartCoroutine(DotBlinkLine("INSERTING CARTRIDGE"));

        // 5번째 줄 - 일반 타이핑
        yield return StartCoroutine(TypeLine("CLEANING METAL CONTACTS..."));
        titleText.text += "\n";
        yield return new WaitForSeconds(lineDelay);

        // 6번째 줄 - 일반 타이핑
        yield return StartCoroutine(TypeLine("LINK CABLE: DISCONNECTED"));
        titleText.text += "\n";
        yield return new WaitForSeconds(lineDelay);

        // 7번째 줄 - FAILED → SUCCESS 연출
        yield return StartCoroutine(TypeLine("CALIBRATING D-PAD... "));
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(TypeLine("FAILED"));
        yield return new WaitForSeconds(0.5f);

        // FAILED 제거 후 SUCCESS 출력
        string current = titleText.text;
        titleText.text = current.Substring(0, current.Length - "FAILED".Length);
        yield return StartCoroutine(TypeLine("SUCCESS"));
        titleText.text += "\n";
        yield return new WaitForSeconds(lineDelay);

        // 8번째 줄 - 로딩 퍼센트 연출
        yield return StartCoroutine(LoadingLine());

        // 빈 줄
        titleText.text += "\n";
        yield return new WaitForSeconds(lineDelay);

        // 마지막 줄
        yield return StartCoroutine(TypeLine("PLEASE WAIT A SECOND..."));

        blink = StartCoroutine(BlinkCursor());
        yield return new WaitForSeconds(2f);
        StopCoroutine(blink);
        titleText.text = titleText.text.TrimEnd('_'); // 혹시 _ 남아있으면 제거

        yield return StartCoroutine(FadeOut(1f));

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(FadeIn(2f));

    }
    private IEnumerator StartGame()
    {
        titlePanel.material = glitchMaterial;
        titlePanel.material.SetFloat("_GlitchIntensity", 0f);

        float intensity = 0f;

        while (intensity < 1f)
        {
            intensity = Mathf.MoveTowards(intensity, 1f, 0.3f * Time.deltaTime);
            titlePanel.material.SetFloat("_GlitchIntensity", intensity);
            yield return null;
        }
        titlePanel.material.SetFloat("_GlitchIntensity", 1f);
    }
    private IEnumerator FadeIn(float duration)
    {
        float elapsed = 0f;
        Color color = Color.white;
        titleText.text = "소년은 끝나지 않는다\n\n\n\n";
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
    // 기본 타이핑 (줄바꿈 없이)
    private IEnumerator TypeLine(string line)
    {
        foreach (char letter in line)
        {
            titleText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    // 배터리 채워지는 연출
    private IEnumerator BatteryLine()
    {
        string prefix = "BATTERY LEVEL: [";
        string suffix = "] 80%";
        int totalBars = 5;
        int filledBars = 4;

        titleText.text += prefix;
        for (int i = 0; i < totalBars; i++)
        {
            titleText.text += (i < filledBars) ? "■" : "□";
            yield return new WaitForSeconds(0.3f);
        }
        titleText.text += suffix + "\n";
        yield return new WaitForSeconds(lineDelay);
    }

    // 점 3개 깜빡이며 추가되는 연출
    private IEnumerator DotBlinkLine(string text)
    {
        titleText.text += text;
        for (int i = 0; i < 3; i++)
        {
            titleText.text += ".";
            yield return new WaitForSeconds(0.3f);
        }
        titleText.text += "\n";
        yield return new WaitForSeconds(lineDelay);
    }

    // 로딩 퍼센트 연출
    private IEnumerator LoadingLine()
    {
        string prefix = "LOADING GAME ASSETS... ";
        titleText.text += prefix;

        int baseLen = titleText.text.Length;
        for (int i = 0; i <= 100; i += 10)
        {
            // 이전 퍼센트 지우고 새로 출력
            titleText.text = titleText.text.Substring(0, baseLen) + i + "%";
            yield return new WaitForSeconds(0.08f);
        }
        titleText.text += "\n";
        yield return new WaitForSeconds(lineDelay);
    }
    IEnumerator BlinkCursor()
    {
        while (true)
        {
            string originalText = titleText.text;
            titleText.text = originalText + "_";
            yield return new WaitForSeconds(0.5f);
            titleText.text = originalText;
            yield return new WaitForSeconds(0.5f);
        }
    }
}