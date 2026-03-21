using UnityEngine;

public enum StageType
{
    Title,
    Tutorial,
    Stage1,
    Stage2,
    Stage3, // 인트로 포함
    Ending
}

public class StageBgmTrigger : MonoBehaviour
{
    [Header("이 씬이 시작될 때 재생할 BGM 선택")]
    public StageType currentStage;

    private void Start()
    {
        if (SoundManager.Instance == null) return;

        switch (currentStage)
        {
            case StageType.Title:
                SoundManager.Instance.PlayTitleBgm(); break;
            case StageType.Tutorial:
                SoundManager.Instance.PlayTutorialBgm(); break;
            case StageType.Stage1:
                SoundManager.Instance.PlayStage1Bgm(); break;
            case StageType.Stage2:
                SoundManager.Instance.PlayStage2Bgm(); break;
            case StageType.Stage3:
                SoundManager.Instance.PlayStage3WithIntro(); break; // 인트로 후 루프 실행
            case StageType.Ending:
                SoundManager.Instance.PlayEndingBgm(); break;
        }
    }
}