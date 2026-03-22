using UnityEngine;
using System.Collections;

// 오디오 클립과 개별 볼륨을 함께 묶어서 인스펙터에 표시하기 위한 클래스입니다.
[System.Serializable]
public class SoundData
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f; // 각 사운드의 고유 볼륨 (기본값 100%)
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM Clips")]
    [SerializeField] private SoundData tutorialBgm;
    [SerializeField] private SoundData titleBgm;
    [SerializeField] private SoundData stage1Bgm;
    [SerializeField] private SoundData stage2Bgm;
    [SerializeField] private SoundData stage3Bgm;
    [SerializeField] private SoundData stage3BossIntroBgm;
    [SerializeField] private SoundData endingBgm;
    [SerializeField] private SoundData gameOverBgm;

    [Header("SFX Clips")]
    [SerializeField] private SoundData buttonClickSfx;
    [SerializeField] private SoundData stunSfx;
    [SerializeField] private SoundData slideSfx;
    [SerializeField] private SoundData climbingSfx;
    [SerializeField] private SoundData coinEatingSfx;

    private float masterBgmVolume = 1f;       // 전체 BGM 마스터 볼륨 설정값
    private float currentBgmVolumeScale = 1f; // 현재 재생중인 BGM의 개별 볼륨값
    private Coroutine bgmTransitionCoroutine; // BGM 자동 전환 코루틴 추적용

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 시작할 때 오디오 소스의 초기 볼륨을 마스터 볼륨으로 저장해둡니다.
        if (bgmSource != null) masterBgmVolume = bgmSource.volume;
    }

    #region BGM

    // bool isLoop = true 매개변수를 추가하여, 값을 넘기지 않으면 기본적으로 반복되도록 설정합니다.
    public void PlayBgm(SoundData data, bool isLoop = true)
    {
        if (data == null || data.clip == null) return;
        
        if (bgmTransitionCoroutine != null)
        {
            StopCoroutine(bgmTransitionCoroutine);
            bgmTransitionCoroutine = null;
        }

        if (bgmSource.clip == data.clip) return;

        currentBgmVolumeScale = data.volume;
        bgmSource.clip = data.clip;
        
        // 고정되어 있던 true 대신 전달받은 isLoop 값을 사용합니다.
        bgmSource.loop = isLoop; 
        
        bgmSource.pitch = 1f; 
        bgmSource.volume = masterBgmVolume * currentBgmVolumeScale; 
        bgmSource.Play();
    }

    public void PlayTutorialBgm() => PlayBgm(tutorialBgm);
    public void PlayTitleBgm() => PlayBgm(titleBgm);
    public void PlayStage1Bgm() => PlayBgm(stage1Bgm);
    public void PlayStage2Bgm() => PlayBgm(stage2Bgm);
    public void PlayStage3Bgm() => PlayBgm(stage3Bgm);
    public void PlayStage3BossIntroBgm() => PlayBgm(stage3BossIntroBgm);
    
    public void PlayStage3WithIntro()
    {
        if (bgmTransitionCoroutine != null) StopCoroutine(bgmTransitionCoroutine);
        bgmTransitionCoroutine = StartCoroutine(Stage3BgmRoutine());
    }

    private IEnumerator Stage3BgmRoutine()
    {
        if (stage3BossIntroBgm != null && stage3BossIntroBgm.clip != null)
        {
            currentBgmVolumeScale = stage3BossIntroBgm.volume;
            bgmSource.clip = stage3BossIntroBgm.clip;
            bgmSource.loop = false; 
            bgmSource.pitch = 1f;
            bgmSource.volume = masterBgmVolume * currentBgmVolumeScale;
            bgmSource.Play();

            yield return new WaitForSeconds(stage3BossIntroBgm.clip.length);
        }

        PlayBgm(stage3Bgm);
    }

    // 엔딩 BGM을 재생할 때만 false를 전달하여 반복을 끕니다.
    public void PlayEndingBgm() => PlayBgm(endingBgm, false);
    
    public void PlayGameOverBgm() => PlayBgm(gameOverBgm);

    #endregion

    #region SFX

    public void PlaySfx(SoundData data)
    {
        if (data == null || data.clip == null) return;

        // PlayOneShot은 (클립, 개별 볼륨 스케일)을 인자로 받습니다. 자동으로 sfxSource.volume과 곱해집니다.
        sfxSource.PlayOneShot(data.clip, data.volume);
    }

    public void PlayButtonClick() => PlaySfx(buttonClickSfx);
    public void PlayStun() => PlaySfx(stunSfx);
    public void PlaySlide() => PlaySfx(slideSfx);
    public void PlayClimbing() => PlaySfx(climbingSfx);
    public void PlayCoinEating() => PlaySfx(coinEatingSfx);

    #endregion

    #region Volume Control

    public void SetBgmVolume(float volume)
    {
        masterBgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null) 
        {
            bgmSource.volume = masterBgmVolume * currentBgmVolumeScale;
        }
    }

    public void SetSfxVolume(float volume)
    {
        if (sfxSource != null) sfxSource.volume = Mathf.Clamp01(volume);
    }

    #endregion
}
