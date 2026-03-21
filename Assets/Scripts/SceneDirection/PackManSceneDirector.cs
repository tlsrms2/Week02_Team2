using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class PackManSceneDirector : MonoBehaviour
{
    public PlayableDirector director;
    public PlayerController playerController;

    public GameObject[] Targets;
    public List<Vector3> TargetPos = new List<Vector3>();
    public GameObject[] coins;
    public Ghost[] ghosts;
    public GameObject panel;
    public GameObject pausePanel;

    public Transform playerStartPoint;

    public ChaserController chaserController;

    public InGameManager gameManager;
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

    public void GlitchMoster()
    {
        chaserController.StartGlitchFade();
    }

    public void GameStart()
    {
        for (int i = 0; i < coins.Length; i++)
        {
            coins[i].gameObject.SetActive(true);
        }

        for (int i = 0; i < ghosts.Length; i++)
        {
            ghosts[i].Init();
        }

        chaserController.SetChasing(true);

    }

    public void GameReStart()
    {
        playerController.gameObject.transform.position = playerStartPoint.position;

        for (int i = 0; i < Targets.Length; i++)
        {
            Targets[i].transform.position = TargetPos[i];
        }
        panel.SetActive(false);
        pausePanel.SetActive(false);
        playerController.Init();

        for (int i = 0; i < coins.Length; i++)
        {
            coins[i].gameObject.SetActive(true);
        }

        for (int i = 0; i < ghosts.Length; i++)
        {
            ghosts[i].Init();
        }
        chaserController.Init();

        gameManager.ResumeGame();
    }

}
