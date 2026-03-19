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

    [Header("방향 노이즈")]
    [Tooltip("랜덤 오차 최대 각도 (도)")]
    [Range(0f, 45f)] public float noiseAngle = 7.5f;
    [Tooltip("노이즈가 변화하는 속도")]
    public float noiseSpeed = 0.8f;

    public Vector3 CurrentGravity { get; private set; }

    private Vector3 _targetDirection;
    private int _activePreset = -1;

    // 노이즈용 내부 상태
    private Vector3 _noiseOffset = Vector3.zero;
    private Vector3 _noiseVelocity = Vector3.zero;
    private float _noiseTimer = 0f;
    private Vector3 _noiseTarget = Vector3.zero;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _targetDirection = gravityDirection.normalized;
        CurrentGravity = Vector3.zero;

        // 첫 노이즈 목표 세팅
        _noiseTarget = RandomNoiseDirection();
    }

    void Update()
    {
        HandleKeyInput();
        UpdateNoise();
        UpdateGravity();
    }

    // ── 노이즈 업데이트 ───────────────────────────
    void UpdateNoise()
    {
        if (noiseAngle <= 0f)
        {
            _noiseOffset = Vector3.zero;
            return;
        }

        _noiseTimer += Time.deltaTime * noiseSpeed;

        // 목표 노이즈에 부드럽게 수렴
        _noiseOffset = Vector3.SmoothDamp(
            _noiseOffset, _noiseTarget,
            ref _noiseVelocity, 1f / noiseSpeed);

        // 목표에 충분히 가까워지면 새 목표 랜덤 생성
        if (Vector3.Distance(_noiseOffset, _noiseTarget) < 0.01f)
            _noiseTarget = RandomNoiseDirection();
    }

    /// <summary>noiseAngle 범위 내 랜덤 방향 오프셋 생성</summary>
    Vector3 RandomNoiseDirection()
    {
        // noiseAngle 범위 내 랜덤 축/각도로 회전
        Vector3 randomAxis = Random.onUnitSphere;
        float randomAngle = Random.Range(-noiseAngle, noiseAngle);
        return Quaternion.AngleAxis(randomAngle, randomAxis) * _targetDirection
               - _targetDirection;
    }

    // ── 중력 업데이트 ─────────────────────────────
    void UpdateGravity()
    {
        Vector3 targetGravity = _activePreset == -1
            ? Vector3.zero
            : (_targetDirection + _noiseOffset).normalized * gravityStrength;

        if (transitionSpeed <= 0f)
        {
            CurrentGravity = targetGravity;
            return;
        }

        CurrentGravity = Vector3.Lerp(
            CurrentGravity, targetGravity,
            Time.deltaTime * transitionSpeed);
    }

    // ── 키 입력 ───────────────────────────────────
    void HandleKeyInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0)) { Deactivate(); return; }

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

        if (_activePreset == presetIndex) Deactivate();
        else Activate(presetIndex, direction);
    }

    void Activate(int presetIndex, Vector3 direction)
    {
        _activePreset = presetIndex;
        _targetDirection = direction.normalized;
        _noiseTarget = RandomNoiseDirection();  // 활성화 시 노이즈 리셋
        Debug.Log($"[GravitySystem] 중력 활성 : Alpha{presetIndex} → {direction}");
    }

    void Deactivate()
    {
        _activePreset = -1;
        Debug.Log("[GravitySystem] 중력 해제");
    }

    public void SetDirection(Vector3 direction)
    {
        _targetDirection = direction.normalized;
        _noiseTarget = RandomNoiseDirection();
    }

    public void SetGravity(Vector3 direction, float strength)
    {
        _targetDirection = direction.normalized;
        gravityStrength = strength;
        _noiseTarget = RandomNoiseDirection();
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (CurrentGravity.sqrMagnitude < 0.01f) return;

        // 실제 중력 방향 (노이즈 포함)
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, CurrentGravity.normalized * 2f);
        Gizmos.DrawSphere(transform.position + CurrentGravity.normalized * 2f, 0.15f);

        // 기준 방향 (노이즈 없는 순수 방향)
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawRay(transform.position, _targetDirection * 2f);
    }
}