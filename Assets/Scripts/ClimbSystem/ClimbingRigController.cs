using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ClimbingRigController : MonoBehaviour
{
    [Header("참조")]
    public Transform body;

    [Header("IK")]
    public TwoBoneIKConstraint leftArmIK, rightArmIK;
    public TwoBoneIKConstraint leftLegIK, rightLegIK;

    [Header("조작")]
    public float mouseSensitivity = 5f;
    public LayerMask wallLayer;

    [Header("리치")]
    public float maxReachArm = 2.4f;
    public float maxReachLeg = 1.8f;
    public float handRadius = 0.15f;
    public float grabRange = 0.6f;

    [Header("몸통 추종")]
    [Range(0f, 1f)] public float bodyFollowWeightXZ = 0.55f;
    [Range(0f, 1f)] public float bodyFollowWeightY = 0.85f;
    public float bodyLerpSpeed = 6f;
    public float rotationSpeed = 4f;

    [Header("벽 오프셋")]
    public float wallStandoffDist = 0.7f;
    public float bodyWallOffset = 0.3f;
    public float normalTrackSpeed = 2f;

    [Header("균형 보정")]
    [Range(0f, 1f)] public float balanceWeight = 0.6f;
    public float maxTiltAngle = 20f;
    public float maxYawAngle = 30f;
    public float tiltSmoothSpeed = 3f;
    public float yawSmoothSpeed = 5f;
    public float rollSmoothSpeed = 2f;

    private float currentTilt = 0f;
    private float currentYaw = 0f;
    private float currentRoll = 0f;

    [Header("자유 사지 추종")]
    public float freeLimbFollowSpeed = 8f;
    public Vector3 freeArmOffset = new Vector3(0.3f, 0.2f, 0.3f);
    public Vector3 freeLegOffset = new Vector3(0.25f, -0.9f, 0.2f);

    [Header("상승 보정")]
    public float upwardBoost = 1.2f;
    public float boostDeadzone = 0.3f;

    [Header("팔 매달림")]
    public float naturalHangLength = 1.1f;

    [Header("다리 바닥 역할")]
    public float minLegBodyGap = 0.4f;
    [Range(0f, 1f)] public float legPushThreshold = 0.5f;

    [Header("플래깅")]
    [Range(0f, 1f)] public float flaggingStrength = 0.25f;
    public float flaggingSpeed = 4f;

    [Header("발/손 회전")]
    public float footRotLerpSpeed = 8f;
    public float toeAngle = 30f;
    public float freeFootRotSpeed = 6f;

    [Header("외력 물리")]
    public float externalDamping = 8f;       // 감쇠 속도 (클수록 빨리 멈춤)
    public float maxExternalSpeed = 10f;      // 최대 외력 속도
    public float anchorPullStrength = 5f;     // 그랩 앵커가 잡아당기는 힘
    [Header("글로벌 중력")]
    public bool useGlobalGravity = true;
    public float gravityScale = 2f;          // 개인 중력 배율 (0이면 무중력)
    public float maxGravitySpeed = 20f;

    private Vector3 _externalVelocity = Vector3.zero;

    // ── 내부 상태 ─────────────────────────────────
    private Vector3 lArmPos, rArmPos, lLegPos, rLegPos;
    private bool lArmGrabbed, rArmGrabbed, lLegGrabbed, rLegGrabbed;
    private TwoBoneIKConstraint activeIK;

    private Vector3 surfaceNormal = Vector3.back;

    // 그랩 시점의 표면 법선 저장
    private Vector3 lArmNormal = Vector3.back;
    private Vector3 rArmNormal = Vector3.back;
    private Vector3 lLegNormal = Vector3.back;
    private Vector3 rLegNormal = Vector3.back;

    private Vector3 dbgBody, dbgCenter;
    private Vector3 _prevBodyPos;
    // ─────────────────────────────────────────────
    void Start()
    {
        var rb = GetComponentInParent<RigBuilder>();
        if (rb != null) rb.Build();

        GrabLimb(leftArmIK, ref lArmPos, ref lArmGrabbed);
        GrabLimb(rightArmIK, ref rArmPos, ref rArmGrabbed);
        GrabLimb(leftLegIK, ref lLegPos, ref lLegGrabbed);
        GrabLimb(rightLegIK, ref rLegPos, ref rLegGrabbed);

        _prevBodyPos = body.position; // ← 추가
        SetCursor(false);
    }

    void Update()
    {
        HandleInput();
        if (activeIK != null) MoveActiveTarget();
        if (Input.GetMouseButtonDown(0)) TryGrab();
    }

    void LateUpdate()
    {
        if (lArmGrabbed) leftArmIK.data.target.position = lArmPos;
        if (rArmGrabbed) rightArmIK.data.target.position = rArmPos;
        if (lLegGrabbed) leftLegIK.data.target.position = lLegPos;
        if (rLegGrabbed) rightLegIK.data.target.position = rLegPos;

        UpdateSurfaceNormal();
        UpdateFreeLimbs();
        ApplyFlagging();
        UpdateBody();
        UpdateRotation();
        UpdateGrabbedLimbRotations();
        UpdateFreeLimbRotations();
    }

    // ── 표면 법선 (단일 레이, 느린 lerp) ────────────
    void UpdateSurfaceNormal()
    {
        Ray ray = new Ray(body.position + surfaceNormal * 0.1f, -surfaceNormal);
        if (Physics.Raycast(ray, out RaycastHit hit,
                            wallStandoffDist * 3f, wallLayer))
        {
            surfaceNormal = Vector3.Slerp(
                surfaceNormal, hit.normal,
                Time.deltaTime * normalTrackSpeed);
        }
    }

    void GetSurfaceTangents(out Vector3 right, out Vector3 up)
    {
        Vector3 worldRef = Mathf.Abs(Vector3.Dot(surfaceNormal, Vector3.up)) > 0.9f
            ? Vector3.forward : Vector3.up;
        right = Vector3.Cross(surfaceNormal, worldRef).normalized;
        up = Vector3.Cross(right, surfaceNormal).normalized;
    }

    // ── 입력 ──────────────────────────────────────
    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Q)) Activate(leftArmIK, ref lArmGrabbed);
        if (Input.GetKeyDown(KeyCode.E)) Activate(rightArmIK, ref rArmGrabbed);
        if (Input.GetKeyDown(KeyCode.A)) Activate(leftLegIK, ref lLegGrabbed);
        if (Input.GetKeyDown(KeyCode.D)) Activate(rightLegIK, ref rLegGrabbed);
    }

    void Activate(TwoBoneIKConstraint ik, ref bool grabbed)
    {
        activeIK = ik;
        grabbed = false;
        SetCursor(true);
    }

    // ── 활성 사지 이동 ─────────────────────────────
    void MoveActiveTarget()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        Transform target = activeIK.data.target;
        Vector3 pos = target.position;

        // ── body 법선이 아닌 타깃 현재 위치의 법선을 구함 ──────
        // body 위치 기준 surfaceNormal로 이동하면
        // 원통 곡면에서 타깃이 점점 안쪽으로 drift하는 원인
        Vector3 localNormal = GetNormalAtPoint(pos);

        // 타깃 위치 법선 기준 접선 좌표계
        Vector3 worldRef = Mathf.Abs(Vector3.Dot(localNormal, Vector3.up)) > 0.9f
            ? Vector3.forward : Vector3.up;
        Vector3 localRight = Vector3.Cross(localNormal, worldRef).normalized;
        Vector3 localUp = Vector3.Cross(localRight, localNormal).normalized;

        // 타깃 위치의 접선 평면 위에서 이동
        pos += localRight * mx + localUp * my;

        // 이동 후 위치에서 다시 법선 구해서 표면 스냅
        Vector3 snapNormal = GetNormalAtPoint(pos);

        Ray ray1 = new Ray(pos + snapNormal * 2f, -snapNormal);
        if (Physics.Raycast(ray1, out RaycastHit hit1, 5f, wallLayer))
        {
            pos = hit1.point + hit1.normal * handRadius;
        }
        else
        {
            // 이미 표면 안쪽인 경우 반대 방향으로 시도
            Ray ray2 = new Ray(pos - snapNormal * 0.1f, snapNormal);
            if (Physics.Raycast(ray2, out RaycastHit hit2, 3f, wallLayer))
                pos = hit2.point + hit2.normal * handRadius;
            else
                pos = target.position; // 실패 시 이동 취소
        }

        // 리치 제한
        float reach = IsLeg(activeIK) ? maxReachLeg : maxReachArm;
        Vector3 toRoot = pos - activeIK.data.root.position;
        if (toRoot.magnitude > reach)
            pos = activeIK.data.root.position + toRoot.normalized * reach;

        target.position = pos;
    }

    // 특정 월드 위치에서 가장 가까운 벽 법선을 구함
    // body surfaceNormal을 힌트로 사용해 레이 방향 결정
    Vector3 GetNormalAtPoint(Vector3 worldPos)
    {
        // 바깥→안쪽
        Ray r1 = new Ray(worldPos + surfaceNormal * 1.5f, -surfaceNormal);
        if (Physics.Raycast(r1, out RaycastHit h1, 4f, wallLayer))
            return h1.normal;

        // 안쪽→바깥쪽 (이미 표면 안에 있을 때)
        Ray r2 = new Ray(worldPos - surfaceNormal * 0.1f, surfaceNormal);
        if (Physics.Raycast(r2, out RaycastHit h2, 3f, wallLayer))
            return h2.normal;

        // 전방향 SphereCast로 가장 가까운 법선 탐색
        RaycastHit[] hits = Physics.SphereCastAll(
            worldPos, 0.5f, Vector3.up, 0.01f, wallLayer);
        if (hits.Length > 0)
            return hits[0].normal;

        // 모두 실패 시 body의 surfaceNormal 사용
        return surfaceNormal;
    }

    // ── 플래깅 ────────────────────────────────────
    void ApplyFlagging()
    {
        if (activeIK == null || flaggingStrength <= 0f) return;

        TwoBoneIKConstraint opp = GetDiagonalOpposite(activeIK);
        if (opp == null) return;
        if (IsLimbGrabbed(opp)) return;
        if (IsLeg(opp) && !IsLeg(activeIK)) return;

        Vector3 activeDir = activeIK.data.target.position - body.position;
        if (activeDir.sqrMagnitude < 0.001f) return;

        Vector3 targetPos = opp.data.target.position
                          + (-activeDir.normalized) * flaggingStrength;

        float reach = IsLeg(opp) ? maxReachLeg : maxReachArm;
        Vector3 toRoot = targetPos - opp.data.root.position;
        if (toRoot.magnitude > reach)
            targetPos = opp.data.root.position + toRoot.normalized * reach;

        opp.data.target.position = Vector3.Lerp(
            opp.data.target.position, targetPos,
            Time.deltaTime * flaggingSpeed);
    }

    // ── 몸통 위치 ─────────────────────────────────
    void UpdateBody()
    {
        // 외력 먼저 적용
        ApplyExternalForce();
        var anchors = new List<Vector3>();
        if (lArmGrabbed) anchors.Add(lArmPos);
        if (rArmGrabbed) anchors.Add(rArmPos);
        if (lLegGrabbed) anchors.Add(lLegPos);
        if (rLegGrabbed) anchors.Add(rLegPos);
        if (anchors.Count == 0) return;

        Vector3 center = Vector3.zero;
        foreach (var p in anchors) center += p;
        center /= anchors.Count;
        dbgCenter = center;

        float targetX = center.x;
        if (activeIK != null)
            targetX = Mathf.Lerp(center.x,
                                 activeIK.data.target.position.x,
                                 bodyFollowWeightXZ);

        float targetY = ComputeBodyY();

        Vector3 desired = new Vector3(targetX, targetY, center.z);
        desired = ApplyWallStandoff(desired);

        if (lArmGrabbed) desired = ClampToLimb3D(desired, leftArmIK, maxReachArm);
        if (rArmGrabbed) desired = ClampToLimb3D(desired, rightArmIK, maxReachArm);
        if (lLegGrabbed) desired = ClampToLimb3D(desired, leftLegIK, maxReachLeg);
        if (rLegGrabbed) desired = ClampToLimb3D(desired, rightLegIK, maxReachLeg);

        dbgBody = desired;
        body.position = Vector3.Lerp(body.position, desired,
                                     Time.deltaTime * bodyLerpSpeed);
    }
    //─────────────────────────────────────────────────────
    //외력 처리 코드
    void ApplyExternalForce()
    {
        // ── 글로벌 중력 누적 ──────────────────────────────
        if (useGlobalGravity && GravitySystem.Instance != null)
        {
            Vector3 gravity = GravitySystem.Instance.CurrentGravity * gravityScale;
            _externalVelocity += gravity * Time.deltaTime;
            _externalVelocity = Vector3.ClampMagnitude(_externalVelocity, maxGravitySpeed);
        }
        // ─────────────────────────────────────────

        if (_externalVelocity.sqrMagnitude < 0.0001f) return;

        // body 이동
        body.position += _externalVelocity * Time.deltaTime;

        // 그랩된 앵커가 있으면 rubber band처럼 당겨서 속도 감쇠 추가
        int anchorCount = 0;
        Vector3 anchorCenter = Vector3.zero;
        if (lArmGrabbed) { anchorCenter += lArmPos; anchorCount++; }
        if (rArmGrabbed) { anchorCenter += rArmPos; anchorCount++; }
        if (lLegGrabbed) { anchorCenter += lLegPos; anchorCount++; }
        if (rLegGrabbed) { anchorCenter += rLegPos; anchorCount++; }

        if (anchorCount > 0)
        {
            anchorCenter /= anchorCount;
            Vector3 toAnchor = anchorCenter - body.position;
            // 앵커 방향으로 속도를 감쇠
            _externalVelocity += toAnchor.normalized
                               * anchorPullStrength
                               * Time.deltaTime;
        }

        // 기본 감쇠
        _externalVelocity = Vector3.Lerp(
            _externalVelocity, Vector3.zero,
            Time.deltaTime * externalDamping);
    }
    /// <summary>
    /// 외부에서 body에 즉각적인 충격을 가합니다.
    /// direction은 월드 방향, magnitude는 세기입니다.
    /// </summary>
    public void AddImpact(Vector3 direction, float magnitude)
    {
        Vector3 force = direction.normalized * magnitude;
        _externalVelocity += force;
        _externalVelocity = Vector3.ClampMagnitude(_externalVelocity, maxExternalSpeed);
    }
    // <summary>
    /// GravityZone에서 FixedUpdate마다 호출 — 누적 중력 적용
    /// </summary>
    public void AddContinuousForce(Vector3 force, float speedCap)
    {
        _externalVelocity += force * Time.fixedDeltaTime;
        _externalVelocity = Vector3.ClampMagnitude(_externalVelocity, speedCap);
    }

    /// <summary>
    /// 구역 퇴장 시 속도 초기화
    /// </summary>
    public void ResetExternalVelocity()
    {
        _externalVelocity = Vector3.zero;
    }
    //─────────────────────────────────────────────────────

    /// <summary>
    /// 특정 월드 위치에서 폭발 범위 안에 있으면 자동으로 세기 계산 후 밀기
    /// </summary>
    public void AddExplosion(Vector3 origin, float radius, float maxMagnitude)
    {
        float dist = Vector3.Distance(body.position, origin);
        if (dist > radius) return;

        float ratio = 1f - (dist / radius);           // 가까울수록 강함
        Vector3 dir = (body.position - origin).normalized;
        AddImpact(dir, maxMagnitude * ratio);
    }

    Vector3 ApplyWallStandoff(Vector3 pos)
    {
        float totalDist = wallStandoffDist + bodyWallOffset;

        Ray ray = new Ray(pos + surfaceNormal * (totalDist + 0.5f), -surfaceNormal);

        if (Physics.Raycast(ray, out RaycastHit hit,
                            totalDist + 2f, wallLayer))
        {
            return hit.point + hit.normal * totalDist;
        }

        return pos + surfaceNormal * bodyWallOffset;
    }

    // ── Y 계산 ────────────────────────────────────
    float ComputeBodyY()
    {
        float armAnchorY = 0f;
        int armCount = 0;
        if (lArmGrabbed) { armAnchorY += lArmPos.y; armCount++; }
        if (rArmGrabbed) { armAnchorY += rArmPos.y; armCount++; }

        float baseY = armCount > 0
            ? armAnchorY / armCount - naturalHangLength
            : body.position.y;

        float targetY = baseY;

        if (activeIK != null)
        {
            float activeY = activeIK.data.target.position.y;
            if (!IsLeg(activeIK))
            {
                float desiredFromArm = activeY - naturalHangLength;
                targetY = Mathf.Lerp(baseY, desiredFromArm, bodyFollowWeightY);

                float heightDiff = activeY - body.position.y;
                if (heightDiff > boostDeadzone)
                    targetY += (heightDiff - boostDeadzone) * upwardBoost;
            }
            else
            {
                targetY = Mathf.Lerp(baseY, activeY + minLegBodyGap, 0.05f);
            }
        }

        if (lLegGrabbed) targetY = Mathf.Max(targetY, lLegPos.y);
        if (rLegGrabbed) targetY = Mathf.Max(targetY, rLegPos.y);

        return targetY;
    }

    // ── 몸통 회전 ─────────────────────────────────
    void UpdateRotation()
    {
        GetSurfaceTangents(out Vector3 surfRight, out Vector3 surfUp);

        Quaternion baseRot = Quaternion.LookRotation(-surfaceNormal, surfUp);

        // Tilt (Z축 기울기)
        float targetTilt = 0f;
        {
            float leftHeight = 0f; int leftCount = 0;
            float rightHeight = 0f; int rightCount = 0;

            if (lArmGrabbed) { leftHeight += Vector3.Dot(lArmPos, surfUp); leftCount++; }
            if (lLegGrabbed) { leftHeight += Vector3.Dot(lLegPos, surfUp); leftCount++; }
            if (rArmGrabbed) { rightHeight += Vector3.Dot(rArmPos, surfUp); rightCount++; }
            if (rLegGrabbed) { rightHeight += Vector3.Dot(rLegPos, surfUp); rightCount++; }

            if (leftCount > 0 && rightCount > 0)
            {
                leftHeight /= leftCount;
                rightHeight /= rightCount;
                float heightDiff = leftHeight - rightHeight;
                targetTilt = Mathf.Clamp(heightDiff * 15f,
                                         -maxTiltAngle, maxTiltAngle);
            }
            else if (leftCount > 0)
                targetTilt = Mathf.Clamp(-8f, -maxTiltAngle, maxTiltAngle);
            else if (rightCount > 0)
                targetTilt = Mathf.Clamp(8f, -maxTiltAngle, maxTiltAngle);
        }

        // Yaw (Y축 회전)
        float targetYaw = 0f;
        {
            if (activeIK != null)
            {
                Vector3 toActive = activeIK.data.target.position - body.position;
                float rawYaw = Vector3.Dot(toActive, surfRight) * 15f;

                Vector3 grabCenter = Vector3.zero;
                int grabCount = 0;
                if (lArmGrabbed) { grabCenter += lArmPos; grabCount++; }
                if (rArmGrabbed) { grabCenter += rArmPos; grabCount++; }
                if (lLegGrabbed) { grabCenter += lLegPos; grabCount++; }
                if (rLegGrabbed) { grabCenter += rLegPos; grabCount++; }

                if (grabCount > 0)
                {
                    grabCenter /= grabCount;
                    float centerOffset = Vector3.Dot(
                        grabCenter - body.position, surfRight);
                    rawYaw = Mathf.Lerp(rawYaw, centerOffset * 10f, 0.4f);
                }

                targetYaw = Mathf.Clamp(rawYaw, -maxYawAngle, maxYawAngle);
            }
        }

        // Roll (X축 앞뒤 기울기)
        float targetRoll = 0f;
        {
            float armHeight = 0f; int armCount_ = 0;
            float legHeight = 0f; int legCount_ = 0;

            if (lArmGrabbed) { armHeight += Vector3.Dot(lArmPos, surfUp); armCount_++; }
            if (rArmGrabbed) { armHeight += Vector3.Dot(rArmPos, surfUp); armCount_++; }
            if (lLegGrabbed) { legHeight += Vector3.Dot(lLegPos, surfUp); legCount_++; }
            if (rLegGrabbed) { legHeight += Vector3.Dot(rLegPos, surfUp); legCount_++; }

            if (armCount_ > 0 && legCount_ > 0)
            {
                armHeight /= armCount_;
                legHeight /= legCount_;
                float spread = armHeight - legHeight;
                targetRoll = Mathf.Clamp(-spread * 3f, -15f, 10f);
            }
        }

        // 개별 스무딩
        currentTilt = Mathf.Lerp(currentTilt, targetTilt,
                                 Time.deltaTime * tiltSmoothSpeed);
        currentYaw = Mathf.Lerp(currentYaw, targetYaw,
                                 Time.deltaTime * yawSmoothSpeed);
        currentRoll = Mathf.Lerp(currentRoll, targetRoll,
                                 Time.deltaTime * rollSmoothSpeed);

        float appliedTilt = currentTilt * balanceWeight;
        float appliedRoll = currentRoll * balanceWeight;

        Quaternion finalRot = baseRot
                            * Quaternion.Euler(appliedRoll, currentYaw, appliedTilt);

        body.rotation = Quaternion.Slerp(body.rotation, finalRot,
                                         Time.deltaTime * rotationSpeed);
    }

    // ── 그랩된 발/손 회전 ─────────────────────────
    void UpdateGrabbedLimbRotations()
    {
        if (lLegGrabbed) ApplyFootRotation(leftLegIK, lLegNormal);
        if (rLegGrabbed) ApplyFootRotation(rightLegIK, rLegNormal);
    }

    void ApplyFootRotation(TwoBoneIKConstraint ik, Vector3 holdNormal)
    {
        Transform target = ik.data.target;

        // 발바닥 윗면 = 표면 법선 (벽을 누르는 방향)
        Vector3 footUp = holdNormal;

        // 발끝 방향: 벽 표면을 따라 위를 향하도록
        GetSurfaceTangents(out Vector3 surfRight, out Vector3 surfUp);
        Vector3 footForward = Vector3.ProjectOnPlane(surfUp, footUp).normalized;

        if (footForward.sqrMagnitude < 0.001f)
            footForward = Vector3.ProjectOnPlane(Vector3.up, footUp).normalized;

        Quaternion baseRot = Quaternion.LookRotation(footForward, footUp);

        // 토우 앵글: 발끝으로 디디는 각도 (클라이머 자세)
        Quaternion toeRotation = Quaternion.Euler(-toeAngle, 0f, 0f);
        Quaternion finalRot = baseRot * toeRotation;

        target.rotation = Quaternion.Slerp(
            target.rotation, finalRot,
            Time.deltaTime * footRotLerpSpeed);
    }

    // ── 자유 사지 회전 (안 잡은 발은 자연스럽게 늘어짐) ──
    void UpdateFreeLimbRotations()
    {
        if (!lLegGrabbed && leftLegIK != activeIK)
            ApplyFreeFootRotation(leftLegIK);
        if (!rLegGrabbed && rightLegIK != activeIK)
            ApplyFreeFootRotation(rightLegIK);
    }

    void ApplyFreeFootRotation(TwoBoneIKConstraint ik)
    {
        Transform target = ik.data.target;

        // 자유 상태 발: 몸통 회전에 맞춰 자연스럽게 아래를 향함
        // forward = 몸통의 forward, up = 몸통의 up
        Vector3 footForward = body.forward;
        Vector3 footUp = body.up;

        // 약간 아래로 처지는 각도
        Quaternion baseRot = Quaternion.LookRotation(footForward, footUp);
        Quaternion droop = Quaternion.Euler(15f, 0f, 0f);
        Quaternion finalRot = baseRot * droop;

        target.rotation = Quaternion.Slerp(
            target.rotation, finalRot,
            Time.deltaTime * freeFootRotSpeed);
    }

    // ── 그랩 ──────────────────────────────────────
    void TryGrab()
    {
        if (activeIK == null) return;

        Vector3 pos = activeIK.data.target.position;
        Vector3 grabNormal = surfaceNormal;

        Collider[] hits = Physics.OverlapSphere(pos, grabRange, wallLayer);
        if (hits.Length > 0)
        {
            Collider col = hits[0];

            // 그랩 지점의 정확한 표면 법선 획득
            Ray normalRay = new Ray(pos + surfaceNormal * 0.5f, -surfaceNormal);
            if (Physics.Raycast(normalRay, out RaycastHit normalHit, 2f, wallLayer))
                grabNormal = normalHit.normal;

            bool canUseClosestPoint = col is BoxCollider
                                   || col is SphereCollider
                                   || col is CapsuleCollider
                                   || (col is MeshCollider mc && mc.convex);

            if (canUseClosestPoint)
            {
                Vector3 contact = Physics.ClosestPoint(pos, col,
                                      col.transform.position,
                                      col.transform.rotation);
                pos = contact + (pos - contact).normalized * handRadius;
            }
        }

        // 법선 저장 후 그랩 확정
        StoreGrabNormal(activeIK, grabNormal);
        GrabAt(activeIK, pos);
        activeIK.data.target.position = pos;
        activeIK = null;
        SetCursor(false);
    }

    void StoreGrabNormal(TwoBoneIKConstraint ik, Vector3 normal)
    {
        if (ik == leftArmIK) lArmNormal = normal;
        else if (ik == rightArmIK) rArmNormal = normal;
        else if (ik == leftLegIK) lLegNormal = normal;
        else if (ik == rightLegIK) rLegNormal = normal;
    }

    void GrabAt(TwoBoneIKConstraint ik, Vector3 pos)
    {
        if (ik == leftArmIK) { lArmPos = pos; lArmGrabbed = true; }
        else if (ik == rightArmIK) { rArmPos = pos; rArmGrabbed = true; }
        else if (ik == leftLegIK) { lLegPos = pos; lLegGrabbed = true; }
        else if (ik == rightLegIK) { rLegPos = pos; rLegGrabbed = true; }
    }

    void GrabLimb(TwoBoneIKConstraint ik, ref Vector3 lockPos, ref bool grabbed)
    {
        if (ik == null) return;
        Vector3 pos = ik.data.tip.position;
        ik.data.target.position = pos;
        lockPos = pos;
        grabbed = true;

        // 초기 그랩 시에도 표면 법선 저장
        Ray ray = new Ray(pos + surfaceNormal * 0.5f, -surfaceNormal);
        if (Physics.Raycast(ray, out RaycastHit hit, 2f, wallLayer))
            StoreGrabNormal(ik, hit.normal);
        else
            StoreGrabNormal(ik, surfaceNormal);
    }

    // ── 자유 사지 추종 ─────────────────────────────
    void UpdateFreeLimbs()
    {
        // body가 이동한 delta
        Vector3 bodyDelta = body.position - _prevBodyPos;
        _prevBodyPos = body.position;

        UpdateFreeLimb(leftArmIK, false, bodyDelta);
        UpdateFreeLimb(rightArmIK, false, bodyDelta);
        UpdateFreeLimb(leftLegIK, true, bodyDelta);
        UpdateFreeLimb(rightLegIK, true, bodyDelta);
    }

    void UpdateFreeLimb(TwoBoneIKConstraint ik, bool isLeg, Vector3 bodyDelta)
    {
        if (ik == null) return;
        if (IsLimbGrabbed(ik)) return;
        if (ik == activeIK) return;

        // ── 핵심 ──────────────────────────────────────────────
        // 자연 위치를 계산해서 lerp하지 않음
        // body가 움직인 만큼만 같이 이동 → 월드 위치 유지
        // 끌려오는 경향 완전 제거
        // ─────────────────────────────────────────────────────
        ik.data.target.position += bodyDelta;

        // 리치 초과 시에만 클램프 (너무 멀어지면 당겨옴)
        float reach = isLeg ? maxReachLeg : maxReachArm;
        Vector3 toRoot = ik.data.target.position - ik.data.root.position;
        if (toRoot.magnitude > reach)
            ik.data.target.position = ik.data.root.position
                                     + toRoot.normalized * reach;
    }
    // ── 유틸리티 ──────────────────────────────────
    Vector3 ClampToLimb3D(Vector3 desiredBody, TwoBoneIKConstraint ik, float reach)
    {
        Vector3 offset = ik.data.root.position - body.position;
        Vector3 futureJoint = desiredBody + offset;
        Vector3 toTip = futureJoint - ik.data.target.position;

        if (toTip.magnitude > reach)
            desiredBody = ik.data.target.position
                        + toTip.normalized * reach
                        - offset;
        return desiredBody;
    }

    TwoBoneIKConstraint GetDiagonalOpposite(TwoBoneIKConstraint ik)
    {
        if (ik == leftArmIK) return rightLegIK;
        if (ik == rightArmIK) return leftLegIK;
        if (ik == leftLegIK) return rightArmIK;
        if (ik == rightLegIK) return leftArmIK;
        return null;
    }

    bool IsLimbGrabbed(TwoBoneIKConstraint ik)
    {
        if (ik == leftArmIK) return lArmGrabbed;
        if (ik == rightArmIK) return rArmGrabbed;
        if (ik == leftLegIK) return lLegGrabbed;
        if (ik == rightLegIK) return rLegGrabbed;
        return false;
    }

    bool IsLeg(TwoBoneIKConstraint ik) => ik == leftLegIK || ik == rightLegIK;

    void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(dbgBody, 0.2f);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(dbgCenter, 0.15f);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(body.position, surfaceNormal * 0.6f);

        if (activeIK != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(body.position, activeIK.data.target.position);
            TwoBoneIKConstraint opp = GetDiagonalOpposite(activeIK);
            if (opp != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f);
                Gizmos.DrawLine(body.position, opp.data.target.position);
            }
        }
    }
}