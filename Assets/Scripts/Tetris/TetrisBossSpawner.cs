using System.Collections;
using UnityEngine;

public class TetrisBossSpawner : MonoBehaviour
{
    [Header("보스 라인 설정")]
    [Tooltip("가로 1줄 모양의 보스 프리팹을 연결하세요.")]
    public GameObject bossLinePrefab;
    
    [Tooltip("다음 줄이 생성될 때 위로 올라갈 높이(간격)를 설정하세요.")]
    public float ySpacing = 1.0f;

    [Header("타이머(속도) 설정")]
    [Tooltip("몇 초마다 한 줄씩 차오를지 설정하세요. (인스펙터에서 조절 가능)")]
    public float spawnInterval = 3.0f;

    private int spawnedLines = 0;
    private Coroutine spawnRoutine;

    private void Start()
    {
        if (bossLinePrefab == null)
        {
            Debug.LogWarning("[TetrisBossSpawner] 보스 라인 프리팹이 할당되지 않았습니다!");
            return;
        }

        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // 현재 스포너의 위치를 기준으로 ySpacing 만큼 위로 올리며 생성
            Vector3 spawnPosition = transform.position + (Vector3.up * (spawnedLines * ySpacing));
            GameObject newBossLine = Instantiate(bossLinePrefab, spawnPosition, transform.rotation, transform);
            
            spawnedLines++;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 startPos = transform.position;

        // 에디터에서 Y Spacing 간격을 쉽게 확인할 수 있도록 가상의 보스 라인을 10개 정도 미리 그려줍니다.
        for (int i = 0; i < 10; i++)
        {
            Vector3 pos = startPos + (Vector3.up * (i * ySpacing));
            // 가로로 긴 빨간 선을 그려 대략적인 크기와 위치를 표시합니다. (숫자 5f는 가로 길이의 절반을 의미)
            Gizmos.DrawLine(pos + Vector3.left * 5f, pos + Vector3.right * 5f);
        }
        Gizmos.DrawLine(startPos, startPos + (Vector3.up * (9 * ySpacing))); // 중심을 이어주는 수직선
    }
}