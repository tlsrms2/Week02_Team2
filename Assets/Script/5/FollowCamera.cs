using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [Header("참조")]
    public Transform target; // body Transform 할당

    [Header("위치")]
    public Vector3 offset = new Vector3(0f, 1.5f, -3.5f); // 등 뒤 오프셋
    public float followSpeed = 8f;

    [Header("회전")]
    public float mouseSensitivity = 3f;
    public float pitchMin = -30f;
    public float pitchMax = 60f;

    private float yaw;
    private float pitch;

    void Start()
    {
        // 시작 시 target 회전 기준으로 초기화
        yaw = target.eulerAngles.y;
        pitch = 10f;
    }

    void LateUpdate()
    {
        // 마우스 입력 (커서가 잠긴 동안만)
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        }

        // 카메라 회전
        Quaternion camRot = Quaternion.Euler(pitch, yaw, 0f);

        // 목표 위치: target 위치 + 회전된 오프셋
        Vector3 desiredPos = target.position + camRot * offset;

        // 카메라와 target 사이에 벽이 있으면 앞으로 당김
        Vector3 dir = desiredPos - target.position;
        if (Physics.Raycast(target.position, dir.normalized,
                            out RaycastHit hit, dir.magnitude))
            desiredPos = hit.point + hit.normal * 0.2f;

        // 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, desiredPos,
                                          Time.deltaTime * followSpeed);
        transform.LookAt(target.position + Vector3.up * 0.5f);
    }
}