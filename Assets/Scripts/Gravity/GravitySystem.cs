using UnityEngine;

public class GravitySystem : MonoBehaviour
{
    public static GravitySystem Instance { get; private set; }

    [Header("초기 중력")]
    public Vector3 gravityDirection = Vector3.down;
    public float gravityStrength = 9.8f;

    [Header("전환 설정")]
    public float transitionSpeed = 3f;

    [Header("중력 프리셋 (Alpha 4~9)")]
    public Vector3 preset4 = Vector3.down;
    public Vector3 preset5 = Vector3.up;
    public Vector3 preset6 = Vector3.left;
    public Vector3 preset7 = Vector3.right;
    public Vector3 preset8 = Vector3.forward;
    public Vector3 preset9 = Vector3.back;

    public Vector3 CurrentGravity { get; private set; }

    private Vector3 _targetDirection;
    private int _activePreset = -1;   // -1 = 비활성 상태

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 시작 시 중력 비활성화
        _targetDirection = gravityDirection.normalized;
        CurrentGravity = Vector3.zero;
    }

    void Update()
    {
        HandleKeyInput();
        UpdateGravity();
    }

    void HandleKeyInput()
    {
        // Alpha0 : 토글 해제
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Deactivate();
            return;
        }

        TryTogglePreset(KeyCode.Alpha4, 4, preset4);
        TryTogglePreset(KeyCode.Alpha5, 5, preset5);
        TryTogglePreset(KeyCode.Alpha6, 6, preset6);
        TryTogglePreset(KeyCode.Alpha7, 7, preset7);
        TryTogglePreset(KeyCode.Alpha8, 8, preset8);
        TryTogglePreset(KeyCode.Alpha9, 9, preset9);
    }

    void TryTogglePreset(KeyCode key, int presetIndex, Vector3 direction)
    {
        if (!Input.GetKeyDown(key)) return;

        // 같은 키 다시 누르면 해제 (토글)
        if (_activePreset == presetIndex)
        {
            Deactivate();
        }
        else
        {
            Activate(presetIndex, direction);
        }
    }

    void Activate(int presetIndex, Vector3 direction)
    {
        _activePreset = presetIndex;
        _targetDirection = direction.normalized;
        Debug.Log($"[GravitySystem] 중력 활성 : Alpha{presetIndex} → {direction}");
    }

    void Deactivate()
    {
        _activePreset = -1;
        Debug.Log("[GravitySystem] 중력 해제");
    }

    void UpdateGravity()
    {
        // 비활성 상태면 중력 0으로 수렴
        Vector3 targetGravity = _activePreset == -1
            ? Vector3.zero
            : _targetDirection * gravityStrength;

        if (transitionSpeed <= 0f)
        {
            CurrentGravity = targetGravity;
            return;
        }

        CurrentGravity = Vector3.Lerp(
            CurrentGravity, targetGravity,
            Time.deltaTime * transitionSpeed);
    }

    public void SetDirection(Vector3 direction)
    {
        _targetDirection = direction.normalized;
    }

    public void SetGravity(Vector3 direction, float strength)
    {
        _targetDirection = direction.normalized;
        gravityStrength = strength;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (CurrentGravity.sqrMagnitude < 0.01f) return;

        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position;
        Vector3 dir = CurrentGravity.normalized * 2f;
        Gizmos.DrawRay(origin, dir);
        Gizmos.DrawSphere(origin + dir, 0.15f);
    }
}