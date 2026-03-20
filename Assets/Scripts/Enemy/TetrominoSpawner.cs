using UnityEngine;

[System.Serializable]
public struct TetrominoSpawnData
{
    public GameObject prefab;           // 테트리스 블록 프리팹
    public Transform[] spawnPoints;     // 이 프리팹이 소환될 수 있는 전용 스폰 지점들 배열
}

public class TetrominoSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    public TetrominoSpawnData[] tetrominoSpawnData; // 블록별 프리팹 및 스폰 지점 매핑 데이터
    public float spawnInterval = 3.0f;    // 블록이 떨어지는 간격 (초)

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnBlock();
            timer = 0f;
        }
    }

    void SpawnBlock()
    {
        if (tetrominoSpawnData == null || tetrominoSpawnData.Length == 0) return;

        // 랜덤한 스폰 데이터(블록) 선택
        int randomIndex = Random.Range(0, tetrominoSpawnData.Length);
        TetrominoSpawnData data = tetrominoSpawnData[randomIndex];

        if (data.prefab == null || data.spawnPoints == null || data.spawnPoints.Length == 0) return;

        // 해당 블록 전용 스폰 지점 중 하나를 랜덤으로 선택
        int randomSpawnIndex = Random.Range(0, data.spawnPoints.Length);
        Transform spawnPoint = data.spawnPoints[randomSpawnIndex];

        Instantiate(data.prefab, spawnPoint.position, spawnPoint.rotation);
    }
}