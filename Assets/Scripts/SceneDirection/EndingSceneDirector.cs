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
    
    // ── 새로 추가된 부분: 개별 대사 사운드 ──
    [Header("사운드 설정 (선택사항)")]
    [Tooltip("이 대사에만 사용할 특별한 목소리(사운드)를 넣으세요. 비워두면 기본 사운드가 재생됩니다.")]
    public AudioClip customTypeSound;
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

    [Header("사운드 설정 (기본값)")]
    public AudioSource typeAudioSource;
    [Tooltip("프레임에 커스텀 사운드가 없을 때 재생될 기본 사운드입니다.")]
    public AudioClip defaultTypeSound;
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
        if (illustrationImage != null && mainIllustration != null)
        {
            activeZoomCoroutine = StartCoroutine(ZoomImageCoroutine());
        }

        yield return new WaitForSeconds(initialDelay);

        foreach (EndingFrame frame in endingFrames)
        {
            TextMeshProUGUI activeTextUI = frame.useCenterText ? centerTextUI : bottomTextUI;
            activeTextUI.text = ""; 

            if (frame.hideImage)
            {
                StopActiveZoom(); 
                if (illustrationImage != null)
                {
                    illustrationImage.color = new Color(1, 1, 1, 0); 
                }
            }

            // ── 핵심 로직: 현재 프레임에 커스텀 사운드가 있으면 그것을, 없으면 기본 사운드를 선택합니다 ──
            AudioClip soundToPlay = (frame.customTypeSound != null) ? frame.customTypeSound : defaultTypeSound;

            // 대사 타이핑 (선택된 사운드를 같이 넘겨줍니다)
            yield return StartCoroutine(TypeLine(activeTextUI, frame.dialogue, soundToPlay));
            
            yield return new WaitForSeconds(delayBetweenLines);

            activeTextUI.text = ""; 
        }

        StopActiveZoom();
        
        onEndingComplete?.Invoke();
    }

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

    private void StopActiveZoom()
    {
        if (activeZoomCoroutine != null)
        {
            StopCoroutine(activeZoomCoroutine);
            activeZoomCoroutine = null;
        }
    }

    // ── 사운드 매개변수(currentSound)가 추가되었습니다 ──
    private IEnumerator TypeLine(TextMeshProUGUI targetUI, string line, AudioClip currentSound)
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

            // 여기서 넘어온 currentSound를 재생합니다.
            if (typeAudioSource != null && currentSound != null && line[charIndex - 1] != ' ')
            {
                typeAudioSource.pitch = Random.Range(1f - pitchRange, 1f + pitchRange);
                typeAudioSource.PlayOneShot(currentSound);
            }

            yield return new WaitForSeconds(typeSpeed);
        }
    }

    public void LoadTitleScene()
    {
        Time.timeScale = 1f;
        StopAllCoroutines();
        SceneManager.LoadScene(titleSceneName);
    }
}