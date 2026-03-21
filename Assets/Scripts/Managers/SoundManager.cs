using UnityEngine;
using System.Collections;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip tutorialBgm;
    [SerializeField] private AudioClip titleBgm;
    [SerializeField] private AudioClip stage1Bgm;
    [SerializeField] private AudioClip stage2Bgm;
    [SerializeField] private AudioClip stage3Bgm;
    [SerializeField] private AudioClip stage3BossIntroBgm;
    [SerializeField] private AudioClip endingBgm;
    [SerializeField] private AudioClip gameOverBgm;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip buttonClickSfx;
    [SerializeField] private AudioClip stunSfx;
    [SerializeField] private AudioClip slideSfx;
    [SerializeField] private AudioClip climbingSfx;
    [SerializeField] private AudioClip coinEatingSfx;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #region BGM

    public void PlayBgm(AudioClip clip)
    {
        if (bgmSource.clip == clip) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.pitch = 1f; // 기본 pitch
        bgmSource.Play();
    }

    public void PlayTutorialBgm() => PlayBgm(tutorialBgm);
    public void PlayTitleBgm() => PlayBgm(titleBgm);
    public void PlayStage1Bgm() => PlayBgm(stage1Bgm);
    public void PlayStage2Bgm() => PlayBgm(stage2Bgm);
    public void PlayStage3Bgm() => PlayBgm(stage3Bgm);
    public void PlayStage3BossIntroBgm() => PlayBgm(stage3BossIntroBgm);
    public void PlayEndingBgm() => PlayBgm(endingBgm);
    public void PlayGameOverBgm() => PlayBgm(gameOverBgm);

    #endregion

    #region SFX

    public void PlaySfx(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    public void PlayButtonClick() => PlaySfx(buttonClickSfx);
    public void PlayStun() => PlaySfx(stunSfx);
    public void PlaySlide() => PlaySfx(slideSfx);
    public void PlayClimbing() => PlaySfx(climbingSfx);
    public void PlayCoinEating() => PlaySfx(coinEatingSfx);

    #endregion
}
