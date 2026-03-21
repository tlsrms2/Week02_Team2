using UnityEngine;
using System.Collections.Generic;

// 다른 스크립트에서 사운드를 토글(드롭다운) 형식으로 선택할 수 있도록 Enum으로 선언합니다.
// 필요한 사운드 이름들을 여기에 자유롭게 추가하세요.
public enum BGMType
{
    None,
    MainTitle,
    Stage1,
    Stage2,
    Stage3
}

public enum SFXType
{
    None,
    Coin,
    Jump,
    GameOver,
    Click
}

// 인스펙터에서 Enum과 AudioClip을 매핑하기 위한 구조체입니다.
[System.Serializable]
public struct BGMMapping
{
    public BGMType type;
    public AudioClip clip;
}

[System.Serializable]
public struct SFXMapping
{
    public SFXType type;
    public AudioClip clip;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Sound Lists (Inspector에서 설정)")]
    public List<BGMMapping> bgmList = new List<BGMMapping>();
    public List<SFXMapping> sfxList = new List<SFXMapping>();

    // 빠른 검색을 위한 딕셔너리
    private Dictionary<BGMType, AudioClip> bgmDictionary = new Dictionary<BGMType, AudioClip>();
    private Dictionary<SFXType, AudioClip> sfxDictionary = new Dictionary<SFXType, AudioClip>();

    private void Awake()
    {
        // 싱글톤 패턴 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 변경되어도 파괴되지 않음
            InitDictionaries();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitDictionaries()
    {
        foreach (var bgm in bgmList)
        {
            if (!bgmDictionary.ContainsKey(bgm.type) && bgm.clip != null)
                bgmDictionary.Add(bgm.type, bgm.clip);
        }

        foreach (var sfx in sfxList)
        {
            if (!sfxDictionary.ContainsKey(sfx.type) && sfx.clip != null)
                sfxDictionary.Add(sfx.type, sfx.clip);
        }
    }

    public void PlayBGM(BGMType type)
    {
        if (type == BGMType.None)
        {
            bgmSource.Stop();
            return;
        }

        if (bgmDictionary.TryGetValue(type, out AudioClip clip))
        {
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning($"[SoundManager] 지정한 BGM을 찾을 수 없습니다: {type}");
        }
    }

    public void PlaySFX(SFXType type)
    {
        if (type == SFXType.None) return;

        if (sfxDictionary.TryGetValue(type, out AudioClip clip))
        {
            // SFX는 여러 번 겹쳐서 재생될 수 있도록 PlayOneShot 사용
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"[SoundManager] 지정한 SFX를 찾을 수 없습니다: {type}");
        }
    }
}
