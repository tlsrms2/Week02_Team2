using UnityEngine;

public class MazeNode : MonoBehaviour
{
    [Header("연결된 방향 설정 (갈 수 있는 노드를 연결)")]
    public MazeNode upNode;
    public MazeNode downNode;
    public MazeNode leftNode;
    public MazeNode rightNode;

    private void OnDrawGizmos()
    {
        // 에디터에서 포인트의 위치를 파란색 공으로 표시하여 배치하기 쉽게 만듭니다.
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 0.2f);

        // 연결된 노드들을 선으로 이어주어 길이 제대로 이어졌는지 시각적으로 확인합니다.
        Gizmos.color = Color.cyan;
        if (upNode != null) Gizmos.DrawLine(transform.position, upNode.transform.position);
        if (downNode != null) Gizmos.DrawLine(transform.position, downNode.transform.position);
        if (leftNode != null) Gizmos.DrawLine(transform.position, leftNode.transform.position);
        if (rightNode != null) Gizmos.DrawLine(transform.position, rightNode.transform.position);
    }
}