using UnityEngine;
using Unity.Cinemachine; // 시네머신 3.x 전용 네임스페이스

public class CinematicHold : MonoBehaviour
{
    [Header("카메라 설정")]
    [Tooltip("이 홀드를 잡았을 때 켤 연출용 측면 카메라")]
    public CinemachineCamera sideCamera; 
    
    [Tooltip("기본 원통 추적 카메라")]
    public CinemachineCamera defaultCamera; 

    // 플레이어가 이 홀드를 잡는 순간 호출됨
    public void OnGrabbed()
    {
        // Priority(우선순위)가 높은 카메라가 화면을 차지합니다.
        if(sideCamera != null) sideCamera.Priority = 20;   
        if(defaultCamera != null) defaultCamera.Priority = 10;
    }

    // 플레이어가 다른 홀드로 이동해 손을 떼는 순간 호출됨
    public void OnReleased()
    {
        // 원래대로 기본 카메라의 우선순위를 높여 복구합니다.
        if(sideCamera != null) sideCamera.Priority = 10;   
        if(defaultCamera != null) defaultCamera.Priority = 20;
    }
}