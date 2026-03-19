using UnityEngine;

/// <summary>
/// 이 콜라이더 안에 있는 플레이어에게 커스텀 중력을 지속 적용합니다.
/// Collider를 Is Trigger ON으로 설정하세요.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GravityZone : MonoBehaviour
{
    [Header("중력 설정")]
    [Tooltip("중력 방향 (월드 기준, 자동 정규화됨)")]
    public Vector3 gravityDirection = Vector3.down;

    [Tooltip("중력 세기 (m/s²)")]
    public float gravityStrength = 9.8f;

    [Tooltip("최대 낙하 속도 제한")]
    public float maxFallSpeed = 20f;

    [Header("진입/퇴장 전환")]
    [Tooltip("구역 진입 시 기존 외력을 리셋할지 여부")]
    public bool resetOnEnter = false;

    [Header("수동 발동")]
    [Tooltip("이 키를 누르는 동안만 중력 적용 (None이면 항상 적용)")]
    public KeyCode activateKey = KeyCode.None;

    [Header("디버그")]
    public bool showGizmos = true;

    private ClimbingRigController _target;
    private bool _isInside = false;

    void OnTriggerEnter(Collider other)
    {
        var climber = other.GetComponentInParent<ClimbingRigController>();
        if (climber == null) return;

        _target = climber;
        _isInside = true;

        if (resetOnEnter)
            _target.ResetExternalVelocity();
    }

    void OnTriggerExit(Collider other)
    {
        var climber = other.GetComponentInParent<ClimbingRigController>();
        if (climber == null || climber != _target) return;

        _isInside = false;
        _target = null;
    }

    void FixedUpdate()
    {
        if (!_isInside || _target == null) return;

        // 수동 키 설정 시 키 입력 확인
        if (activateKey != KeyCode.None && !Input.GetKey(activateKey)) return;

        Vector3 force = gravityDirection.normalized * gravityStrength;
        _target.AddContinuousForce(force, maxFallSpeed);
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;

        var col = GetComponent<Collider>();
        if (col is BoxCollider box)
            Gizmos.DrawCube(box.center, box.size);
        else if (col is SphereCollider sphere)
            Gizmos.DrawSphere(sphere.center, sphere.radius);

        // 중력 방향 화살표
        Gizmos.color = Color.cyan;
        Gizmos.matrix = Matrix4x4.identity;
        Vector3 origin = transform.position;
        Vector3 dir = gravityDirection.normalized * 1.5f;
        Gizmos.DrawRay(origin, dir);
        Gizmos.DrawSphere(origin + dir, 0.1f);
    }
}