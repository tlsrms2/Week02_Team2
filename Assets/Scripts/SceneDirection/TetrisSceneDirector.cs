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

    void Awake()
    {
        stageBgmTrigger = FindAnyObjectByType<StageBgmTrigger>();
        adSfx = SoundManager.Instance.sfxSource;
        adBgm = SoundManager.Instance.bgmSource;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        director.Play();

        for (int i = 0; i < Targets.Length; i++)
        {
            TargetPos.Add(Targets[i].transform.position);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void StartGame()
    {
        playerController.gameObject.SetActive(true);
        spawnManager.StartGame();
        spawner.StartTetrisBoss();
        foreach (var v in startHolder)
        {
            v.SetActive(true);
        }
    }
    public void ReStartGame()
    {
        playerController.gameObject.transform.position = playerStartPoint.position;

        for (int i = 0; i < Targets.Length; i++)
        {
            Targets[i].transform.position = TargetPos[i];
        }
        panel.SetActive(false);
        pausePanel.SetActive(false);
        playerController.Init();
        adSfx.Stop();
        adBgm.Stop();
        stageBgmTrigger.gameObject.SetActive(false);
        stageBgmTrigger.gameObject.SetActive(true);
        adSfx.Play();
        adBgm.Play();
        spawnManager.ReStartGame();
        spawner.ReStartTetrisBoss();
        gameManager.ResumeGame();
        gameManager.SetGameOver(false);
    }
}
