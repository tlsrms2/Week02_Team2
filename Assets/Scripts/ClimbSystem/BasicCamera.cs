using UnityEngine;

public class BasicCamera : MonoBehaviour
{
    [Header("추적 대상 및 환경")]
    [Tooltip("따라갈 플레이어의 Transform (Body)")]
    public Transform target;
    
    [Tooltip("원통의 중심점 역할을 할 오브젝트")]
    public Transform cylinderCenter;

    [Header("카메라 설정")]
    [Tooltip("카메라가 플레이어의 어느 높이를 바라볼지 (예: 약간 위쪽)")]
    public float lookAtHeightOffset = 0.5f;
    
    [Tooltip("카메라의 상하(Y축) 이동 부드러움 정도")]
    public float followSpeed = 5f;

    void LateUpdate()
    {
        // 타겟이나 중심점 설정이 누락되었다면 에러를 방지하기 위해 작동 중지
        if (target == null || cylinderCenter == null) return;

        // 1. 카메라 위치 계산: X와 Z는 원통 중심 고정, Y는 플레이어의 Y 높이를 사용
        float targetY = target.position.y;
        Vector3 desiredPosition = new Vector3(
            cylinderCenter.position.x, 
            targetY, 
            cylinderCenter.position.z
        );

        // 부드러운 수직 이동 (Lerp: 현재 위치에서 목표 위치로 부드럽게 보간)
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);

        // 2. 카메라 회전: 플레이어의 위치를 향해 회전
        Vector3 lookTarget = target.position + Vector3.up * lookAtHeightOffset;
        transform.LookAt(lookTarget); // LookAt은 지정한 좌표를 향해 오브젝트를 회전시키는 직관적인 함수입니다.
    }
}