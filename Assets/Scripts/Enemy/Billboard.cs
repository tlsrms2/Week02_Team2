using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    [Header("빌보드 설정")]
    [Tooltip("체크하면 위아래로 눕지 않고 항상 꼿꼿하게 서서 카메라를 바라봅니다.")]
    public bool lockYAxis = false;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            if (lockYAxis)
            {
                // 카메라가 내려다보더라도 스프라이트가 뒤로 눕지(비스듬해지지) 않도록 Y축을 고정합니다.
                Vector3 camForward = mainCamera.transform.forward;
                camForward.y = 0;
                transform.forward = camForward.normalized;
            }
            else
            {
                // 스프라이트가 항상 메인 카메라가 바라보는 방향과 동일한 곳을 바라보게 합니다.
                transform.forward = mainCamera.transform.forward;
            }
        }
    }
}