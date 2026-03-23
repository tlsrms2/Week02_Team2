using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class PackManSceneDirector : MonoBehaviour
{
    public PlayableDirector director;

    public Ghost[] ghosts;

    public ChaserController chaserController;

    public InGameManager gameManager;

    public bool debugBool;
    public bool isAlreadyWatch;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int StageNum = 2;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        if (debugBool) return;
        if (!PlayerPrefs.HasKey("CutSceneSeen_" + StageNum))
        {
            // 처음 보는 스테이지 → 컷신 재생
            director.Play();

            // 봤다고 저장
            PlayerPrefs.SetInt("CutSceneSeen_" + StageNum, 1);
            PlayerPrefs.Save();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GlitchMoster()
    {

    }
    public void StopGame()
    {
        for (int i = 0; i < ghosts.Length; i++)
        {
            ghosts[i].Stop();
        }
        chaserController.SetChasing(false);
        chaserController.StartGlitchFade();
    }
    public void GameStart()
    {

        for (int i = 0; i < ghosts.Length; i++)
        {
            ghosts[i].Init();
        }

        chaserController.SetChasing(true);

    }


}
