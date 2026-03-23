using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class TetrisSceneDirector : MonoBehaviour
{
    public PlayableDirector director;
    public PlayerController playerController;

    public GameObject[] Targets;
    public List<Vector3> TargetPos = new List<Vector3>();
    public GameObject panel;
    public GameObject pausePanel;

    public Transform playerStartPoint;
    public InGameManager gameManager;
    public TetrisBossSpawner spawner;
    public SpawnManager spawnManager;

    public List<GameObject> startHolder;

    private AudioSource adSfx;
    private AudioSource adBgm;
    private StageBgmTrigger stageBgmTrigger;

    public bool debugBool;
    public int StageNum = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (debugBool)
        {
            director.Play();
            return;
        }
        if (!PlayerPrefs.HasKey("CutSceneSeen_" + StageNum))
        {
            // 처음 보는 스테이지 → 컷신 재생
            director.Play();

            // 봤다고 저장
            PlayerPrefs.SetInt("CutSceneSeen_" + StageNum, 1);
            PlayerPrefs.Save();
        }
        else
        {
            StartGame();
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    public void StopGame()
    {

    }

    public void StartGame()
    {
        playerController.gameObject.SetActive(true);
        spawnManager.StartGame();
        spawner.StartTetrisBoss();

    }

}
