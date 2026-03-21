using UnityEngine;

public class GroundDirection : MonoBehaviour
{
    [Header("강제 이동 방향 설정")]
    [Tooltip("이 땅을 밟았을 때 강제로 굴러가게 할 방향 (오른쪽: 1,0,0 / 왼쪽: -1,0,0)")]
    public Vector3 forceDirection = Vector3.right;
}