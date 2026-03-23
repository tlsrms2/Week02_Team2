using UnityEngine;
using UnityEngine.Playables;

public class StageSceneDirector : MonoBehaviour
{
    public int StageNum;
    public PlayableDirector director;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(StageCutSceneManager.Instance.StageSeenDict.TryGetValue(StageNum, out bool value))
        {
            director.Play();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
