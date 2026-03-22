using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class DonkeyKongDirector : MonoBehaviour
{
    public PlayableDirector director;
    public PlayerController playerController;

    public GameObject[] Targets;
    public List<Vector3> TargetPos = new List<Vector3>();

    public GameObject panel;
    public GameObject pausePanel;

    public Transform playerStartPoint;

    public ChaserController chaserController;

    public InGameManager gameManager;

    public BarrelSpawner barrelSpawner;

    void Start()
    {

        director.Play();

        for (int i = 0; i < Targets.Length; i++)
        {
            TargetPos.Add(Targets[i].transform.position);
        }

    }


    public void GlitchMoster()
    {
        chaserController.StartGlitchFade();
    }

    public void GameStart()
    {
        barrelSpawner.StartSpawnBarrel();
        chaserController.SetChasing(true);

    }

    public void ActivePlayer()
    {
        playerController.gameObject.SetActive(true);
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

        barrelSpawner.ReStartSpawnBarrel();


        chaserController.Init();

        gameManager.ResumeGame();
        gameManager.SetGameOver(false);
    }
}
