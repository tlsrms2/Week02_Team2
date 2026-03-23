using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class StageCutSceneManager : MonoBehaviour
{
    public Dictionary<int,bool > StageSeenDict = new Dictionary<int,bool>();
    public static StageCutSceneManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
