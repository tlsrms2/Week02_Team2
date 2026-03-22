using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[System.Serializable]
public class EndingFrame
{
    [TextArea(3, 5)]
    public string dialogue;
    [Tooltip("체크하면 가운데 텍스트 UI를, 해제하면 하단 텍스트 UI를 사용합니다.")]
    public bool useCenterText;
    [Tooltip("체크하면 일러스트가 완전히 사라지고 검은 배경만 남습니다. (마지막 연출용)")]
    public bool hideImage;
}

public class EndingSceneDirector : MonoBehaviour
{
    [Header("UI 연결")]
    public Image illustrationImage;
    public TextMeshProUGUI bottomTextUI;
    public TextMeshProUGUI centerTextUI;

    [Header("일러스트 설정")]
    [Tooltip("엔딩 씬 내내 확대되며 보여질 단 하나의 일러스트를 넣으세요.")]
    public Sprite mainIllustration;

    [Header("연출 시간 설정")]
    public float initialDelay = 2.0f;
    public float delayBetweenLines = 1.5f;
    public float typeSpeed = 0.05f;

    [Header("애니메이션 설정")]
    [Tooltip("매 초당 증가할 크기 비율 (예: 0.01은 초당 1%씩 증가)")]
    public float zoomSpeed = 0.01f; 

    [Header("사운드 설정")]
    public AudioSource typeAudioSource;
    public AudioClip typeSound;
    [Range(0f, 0.5f)] public float pitchRange = 0.1f;

    [Header("엔딩 프레임 (씬 구성)")]
    public EndingFrame[] endingFrames;

    [Header("엔딩 종료 이벤트")]
    public UnityEvent onEndingComplete;

    [Header("씬 전환 설정")]
    public string titleSceneName = "TitleScene";

    // 애니메이션 제어용 변수
    private Coroutine activeZoomCoroutine;

    private void Start()
    {
        if (bottomTextUI != null) bottomTextUI.text = "";
        if (centerTextUI != null) centerTextUI.text = "";
        
        // 씬 시작 시 메인 이미지를 할당하고 화면에 표시
        if (illustrationImage != null && mainIllustration != null)
        {
            illustrationImage.sprite = mainIllustration;
            illustrationImage.color = new Color(1, 1, 1, 1);
            illustrationImage.rectTransform.localScale = Vector3.one;
        }
        
        StartCoroutine(PlayEndingSequence());
    }

    private IEnumerator PlayEndingSequence()
    {
        // 대사 출력과 별개로, 씬 시작과 동시에 줌 애니메이션을 단 한 번만 시작
        if (illustrationImage != null && mainIllustration != null)
        {
            activeZoomCoroutine = StartCoroutine(ZoomImageCoroutine());
        }

        yield return new WaitForSeconds(initialDelay);

        foreach (EndingFrame frame in endingFrames)
        {
            TextMeshProUGUI activeTextUI = frame.useCenterText ? centerTextUI : bottomTextUI;
            activeTextUI.text = ""; 

            // 현재 프레임이 이미지를 숨겨야 하는 프레임(마지막 검은 배경)이라면
            if (frame.hideImage)
            {
                StopActiveZoom(); // 줌 멈춤
                if (illustrationImage != null)
                {
                    illustrationImage.color = new Color(1, 1, 1, 0); // 완전 투명화
                }
            }

            // 대사 타이핑
            yield return StartCoroutine(TypeLine(activeTextUI, frame.dialogue));
            
            // 대기 시간
            yield return new WaitForSeconds(delayBetweenLines);

            // 다음 대사를 위해 텍스트 지우기
            activeTextUI.text = ""; 
        }

        // 모든 대사가 끝난 후 안전하게 줌 정지
        StopActiveZoom();
        
        onEndingComplete?.Invoke();
    }

    // 이미지를 끊김없이 계속 키우는 코루틴
    private IEnumerator ZoomImageCoroutine()
    {
        RectTransform rt = illustrationImage.rectTransform;
        
        while (true) 
        {
            float growth = zoomSpeed * Time.deltaTime;
            rt.localScale += new Vector3(growth, growth, 0);
            yield return null; 
        }
    }

    // 줌 코루틴을 멈추는 기능
    private void StopActiveZoom()
    {
        if (activeZoomCoroutine != null)
        {
            StopCoroutine(activeZoomCoroutine);
            activeZoomCoroutine = null;
        }
    }

    private IEnumerator TypeLine(TextMeshProUGUI targetUI, string line)
    {
        int charIndex = 0;
        while (charIndex < line.Length)
        {
            if (line[charIndex] == '<')
            {
                int tagEndIndex = line.IndexOf('>', charIndex);
                if (tagEndIndex != -1)
                {
                    string fullTag = line.Substring(charIndex, tagEndIndex - charIndex + 1);
                    targetUI.text += fullTag;
                    charIndex = tagEndIndex + 1;
                    continue; 
                }
            }

            targetUI.text += line[charIndex];
            charIndex++;

            if (typeAudioSource != null && typeSound != null && line[charIndex - 1] != ' ')
            {
                typeAudioSource.pitch = Random.Range(1f - pitchRange, 1f + pitchRange);
                typeAudioSource.PlayOneShot(typeSound);
            }

            yield return new WaitForSeconds(typeSpeed);
        }
    }

    public void LoadTitleScene()
    {
        SceneManager.LoadScene(titleSceneName);
    }
}