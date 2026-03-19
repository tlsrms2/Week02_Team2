using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ClimbingRigController : MonoBehaviour
{
    // ── 사지 데이터 ───────────────────────────────
    [System.Serializable]
    public class Limb
    {
        public TwoBoneIKConstraint ik;
        public KeyCode key;
        public bool isLeg;
        public Renderer renderer;   // 아웃라인 대상 메쉬 렌더러

        [HideInInspector] public Vector3 grabPos;
        [HideInInspector] public Vector3 grabNormal;
        [HideInInspector] public bool grabbed;

        public float MaxReach(float arm, float leg) => isLeg ? leg : arm;
    }

    [Header("참조")]
    public Transform body;

    [Header("사지 설정")]
    public Limb leftArm = new() { key = KeyCode.Q, isLeg = false };
    public Limb rightArm = new() { key = KeyCode.E, isLeg = false };
    public Limb leftLeg = new() { key = KeyCode.A, isLeg = true };
    public Limb rightLeg = new() { key = KeyCode.D, isLeg = true };

    [Header("조작")]
    public float mouseSensitivity = 5f;
    public LayerMask wallLayer;
    public LayerMask holdLayer;

    [Header("리치")]
    public float maxReachArm = 2.4f;
    public float maxReachLeg = 1.8f;
    public float handRadius = 0.15f;
    public float grabRange = 0.6f;

    [Header("몸통")]
    [Range(0f, 1f)] public float bodyFollowWeight = 0.55f;
    public float bodyLerpSpeed = 6f;
    public float rotationSpeed = 4f;
    public float wallStandoffDist = 0.7f;
    public float bodyWallOffset = 0.3f;
    public float normalTrackSpeed = 2f;
    public float naturalHangLength = 1.1f;

    [Header("균형")]
    [Range(0f, 1f)] public float balanceWeight = 0.6f;
    public float maxTiltAngle = 20f;

    [Header("아웃라인")]
    public Material outlineMaterial;

    [Header("슬라이드")]
    public float slideSpeed = 3f;
    public float slideDamping = 5f;
    [Range(0f, 1f)] public float armSlideResistance = 0.7f;

    // ── 내부 상태 ─────────────────────────────────
    private Limb[] limbs;
    private Limb activeLimb;
    private Vector3 surfaceNormal = Vector3.back;
    private Vector3 prevBodyPos;

    private LayerMask combinedLayer;
    private float slideVelocity;
    private float stretchVelocity;

    // ───────────────────────────────────────────────
    void Start()
    {
        limbs = new[] { leftArm, rightArm, leftLeg, rightLeg };
        combinedLayer = wallLayer | holdLayer;

        GetComponentInParent<RigBuilder>()?.Build();

        foreach (var limb in limbs) InitGrab(limb);

        prevBodyPos = body.position;
        SetCursor(false);
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            Slide(6f);
        }
        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            SlideStretch(6f);
        }
        // 사지 선택
        foreach (var limb in limbs)
        {
            if (Input.GetKeyDown(limb.key))
            {
                if (activeLimb != null) RemoveOutline(activeLimb);
                activeLimb = limb;
                limb.grabbed = false;
                AddOutline(limb);
                SetCursor(true);
            }
        }

        // 활성 사지 마우스 이동
        if (activeLimb != null) MoveActiveLimb();

        // 클릭으로 그랩
        if (Input.GetMouseButtonDown(0)) TryGrab();
    }

    void LateUpdate()
    {
        ProcessSlide();
        ProcessStretchSlide();

        // 그랩된 타겟 고정
        foreach (var limb in limbs)
            if (limb.grabbed)
                limb.ik.data.target.position = limb.grabPos;

        UpdateSurfaceNormal();
        UpdateFreeLimbs();
        UpdateBody();
        UpdateRotation();
    }

    // ── 슬라이드 ────────────────────────────────────
    /// <summary>
    /// 외부에서 호출. distance만큼 벽 표면을 따라 아래로 미끄러집니다.
    /// </summary>
    public void Slide(float distance)
    {
        slideVelocity += distance * slideSpeed;
    }

    void ProcessSlide()
    {
        if (Mathf.Abs(slideVelocity) < 0.001f) return;

        GetSurfaceBasis(out _, out var surfUp);
        float step = slideVelocity * Time.deltaTime;
        Vector3 slideDelta = -surfUp * step;

        foreach (var limb in limbs)
        {
            if (!limb.grabbed) continue;

            Vector3 newPos = limb.grabPos + slideDelta;

            // 벽 표면에 다시 붙이기
            var ray = new Ray(newPos + surfaceNormal * 1f, -surfaceNormal);
            if (Physics.Raycast(ray, out var hit, 3f, combinedLayer))
                newPos = hit.point + hit.normal * handRadius;

            limb.grabPos = newPos;
        }

        slideVelocity = Mathf.Lerp(slideVelocity, 0f,
                                    Time.deltaTime * slideDamping);
    }

    /// <summary>
    /// 외부에서 호출. 팔다리가 위로 당겨지며 미끄러집니다.
    /// </summary>
    public void SlideStretch(float distance)
    {
        stretchVelocity += distance * slideSpeed;
    }

    void ProcessStretchSlide()
    {
        if (Mathf.Abs(stretchVelocity) < 0.001f) return;

        GetSurfaceBasis(out _, out var surfUp);
        float step = stretchVelocity * Time.deltaTime;
        Vector3 delta = -surfUp * step;

        // 다리: 전속으로 끌려감 / 팔: 저항하며 천천히 끌려감
        foreach (var limb in limbs)
        {
            if (!limb.grabbed) continue;

            float rate = limb.isLeg ? 1f : (1f - armSlideResistance);
            Vector3 newPos = limb.grabPos + delta * rate;

            var ray = new Ray(newPos + surfaceNormal * 1f, -surfaceNormal);
            if (Physics.Raycast(ray, out var hit, 3f, combinedLayer))
                newPos = hit.point + hit.normal * handRadius;

            limb.grabPos = newPos;
        }

        stretchVelocity = Mathf.Lerp(stretchVelocity, 0f,
                                      Time.deltaTime * slideDamping);
    }

    // ── 표면 법선 ─────────────────────────────────
    void UpdateSurfaceNormal()
    {
        var ray = new Ray(body.position + surfaceNormal * 0.1f, -surfaceNormal);
        if (Physics.Raycast(ray, out var hit, wallStandoffDist * 3f, wallLayer))
            surfaceNormal = Vector3.Slerp(surfaceNormal, hit.normal,
                                          Time.deltaTime * normalTrackSpeed);
    }

    void GetSurfaceBasis(out Vector3 right, out Vector3 up)
    {
        var worldRef = Mathf.Abs(Vector3.Dot(surfaceNormal, Vector3.up)) > 0.9f
            ? Vector3.forward : Vector3.up;
        right = Vector3.Cross(surfaceNormal, worldRef).normalized;
        up = Vector3.Cross(right, surfaceNormal).normalized;
    }

    // ── 활성 사지 이동 ────────────────────────────
    void MoveActiveLimb()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        var target = activeLimb.ik.data.target;
        var pos = target.position;

        // 표면 접선 기반 이동
        var normal = GetNormalAt(pos);
        var worldRef = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.9f
            ? Vector3.forward : Vector3.up;
        var right = Vector3.Cross(normal, worldRef).normalized;
        var up = Vector3.Cross(right, normal).normalized;

        pos += right * mx + up * my;

        // 벽에 스냅
        pos = SnapToSurface(pos, GetNormalAt(pos), target.position);

        // 리치 제한
        pos = ClampReach(pos, activeLimb);

        target.position = pos;
    }

    Vector3 SnapToSurface(Vector3 pos, Vector3 normal, Vector3 fallback)
    {
        var ray = new Ray(pos + normal * 2f, -normal);
        if (Physics.Raycast(ray, out var hit, 5f, combinedLayer))
            return hit.point + hit.normal * handRadius;

        ray = new Ray(pos - normal * 0.1f, normal);
        if (Physics.Raycast(ray, out hit, 3f, combinedLayer))
            return hit.point + hit.normal * handRadius;

        return fallback;
    }

    Vector3 GetNormalAt(Vector3 pos)
    {
        var ray = new Ray(pos + surfaceNormal * 1.5f, -surfaceNormal);
        if (Physics.Raycast(ray, out var hit, 4f, combinedLayer))
            return hit.normal;

        ray = new Ray(pos - surfaceNormal * 0.1f, surfaceNormal);
        if (Physics.Raycast(ray, out hit, 3f, combinedLayer))
            return hit.normal;

        return surfaceNormal;
    }

    // ── 그랩 ──────────────────────────────────────
    void TryGrab()
    {
        if (activeLimb == null) return;

        var pos = activeLimb.ik.data.target.position;
        var hits = Physics.OverlapSphere(pos, grabRange, holdLayer);
        if (hits.Length == 0) return;

        var col = hits[0];

        // 접촉점 보정
        bool canSnap = col is BoxCollider or SphereCollider or CapsuleCollider
                    || (col is MeshCollider mc && mc.convex);
        if (canSnap)
        {
            var contact = Physics.ClosestPoint(pos, col,
                              col.transform.position, col.transform.rotation);
            pos = contact + (pos - contact).normalized * handRadius;
        }

        // 법선 저장
        var ray = new Ray(pos + surfaceNormal * 0.5f, -surfaceNormal);
        activeLimb.grabNormal = Physics.Raycast(ray, out var hit, 2f, combinedLayer)
            ? hit.normal : surfaceNormal;

        activeLimb.grabPos = pos;
        activeLimb.grabbed = true;
        activeLimb.ik.data.target.position = pos;
        RemoveOutline(activeLimb);
        activeLimb = null;
        SetCursor(false);
    }

    void InitGrab(Limb limb)
    {
        if (limb.ik == null) return;
        var pos = limb.ik.data.tip.position;
        limb.ik.data.target.position = pos;
        limb.grabPos = pos;
        limb.grabbed = true;

        var ray = new Ray(pos + surfaceNormal * 0.5f, -surfaceNormal);
        limb.grabNormal = Physics.Raycast(ray, out var hit, 2f, wallLayer)
            ? hit.normal : surfaceNormal;
    }

    // ── 자유 사지 추종 ────────────────────────────
    void UpdateFreeLimbs()
    {
        var delta = body.position - prevBodyPos;
        prevBodyPos = body.position;

        foreach (var limb in limbs)
        {
            if (limb.grabbed || limb == activeLimb) continue;

            limb.ik.data.target.position += delta;
            limb.ik.data.target.position =
                ClampReach(limb.ik.data.target.position, limb);
        }
    }

    // ── 몸통 위치 ─────────────────────────────────
    void UpdateBody()
    {
        var anchors = new List<Vector3>();
        foreach (var limb in limbs)
            if (limb.grabbed) anchors.Add(limb.grabPos);
        if (anchors.Count == 0) return;

        // 앵커 중심
        var center = Vector3.zero;
        foreach (var p in anchors) center += p;
        center /= anchors.Count;

        // X: 활성 사지 방향으로 약간 치우침
        float targetX = center.x;
        if (activeLimb != null)
            targetX = Mathf.Lerp(center.x,
                      activeLimb.ik.data.target.position.x,
                      bodyFollowWeight);

        // Y: 팔 앵커 기준 매달림
        float targetY = ComputeBodyY();

        var desired = new Vector3(targetX, targetY, center.z);
        desired = ApplyWallStandoff(desired);

        // 리치 제한
        foreach (var limb in limbs)
            if (limb.grabbed)
                desired = ClampBodyToLimb(desired, limb);

        body.position = Vector3.Lerp(body.position, desired,
                                     Time.deltaTime * bodyLerpSpeed);
    }

    float ComputeBodyY()
    {
        float armY = 0f; int armCount = 0;
        foreach (var limb in limbs)
        {
            if (!limb.grabbed || limb.isLeg) continue;
            armY += limb.grabPos.y;
            armCount++;
        }

        float baseY = armCount > 0
            ? armY / armCount - naturalHangLength
            : body.position.y;

        // 활성 팔이 위로 가면 몸도 따라 올라감
        if (activeLimb != null && !activeLimb.isLeg)
        {
            float activeY = activeLimb.ik.data.target.position.y;
            baseY = Mathf.Lerp(baseY, activeY - naturalHangLength, 0.85f);
        }

        // 다리 앵커보다 아래로 내려가지 않음
        foreach (var limb in limbs)
            if (limb.grabbed && limb.isLeg)
                baseY = Mathf.Max(baseY, limb.grabPos.y);

        return baseY;
    }

    Vector3 ApplyWallStandoff(Vector3 pos)
    {
        float dist = wallStandoffDist + bodyWallOffset;
        var ray = new Ray(pos + surfaceNormal * (dist + 0.5f), -surfaceNormal);

        if (Physics.Raycast(ray, out var hit, dist + 2f, wallLayer))
            return hit.point + hit.normal * dist;

        return pos + surfaceNormal * bodyWallOffset;
    }

    // ── 몸통 회전 ─────────────────────────────────
    void UpdateRotation()
    {
        GetSurfaceBasis(out var surfRight, out var surfUp);
        var baseRot = Quaternion.LookRotation(-surfaceNormal, surfUp);

        // 좌우 높이차로 기울기
        float tilt = 0f;
        float leftH = 0f, rightH = 0f;
        int lc = 0, rc = 0;

        if (leftArm.grabbed) { leftH += Vector3.Dot(leftArm.grabPos, surfUp); lc++; }
        if (leftLeg.grabbed) { leftH += Vector3.Dot(leftLeg.grabPos, surfUp); lc++; }
        if (rightArm.grabbed) { rightH += Vector3.Dot(rightArm.grabPos, surfUp); rc++; }
        if (rightLeg.grabbed) { rightH += Vector3.Dot(rightLeg.grabPos, surfUp); rc++; }

        if (lc > 0 && rc > 0)
            tilt = Mathf.Clamp((leftH / lc - rightH / rc) * 15f,
                               -maxTiltAngle, maxTiltAngle);

        var finalRot = baseRot * Quaternion.Euler(0f, 0f, tilt * balanceWeight);
        body.rotation = Quaternion.Slerp(body.rotation, finalRot,
                                         Time.deltaTime * rotationSpeed);
    }

    // ── 유틸 ──────────────────────────────────────
    Vector3 ClampReach(Vector3 pos, Limb limb)
    {
        float reach = limb.MaxReach(maxReachArm, maxReachLeg);
        var toRoot = pos - limb.ik.data.root.position;
        if (toRoot.magnitude > reach)
            pos = limb.ik.data.root.position + toRoot.normalized * reach;
        return pos;
    }

    Vector3 ClampBodyToLimb(Vector3 desiredBody, Limb limb)
    {
        float reach = limb.MaxReach(maxReachArm, maxReachLeg);
        var offset = limb.ik.data.root.position - body.position;
        var futureJoint = desiredBody + offset;
        var toTip = futureJoint - limb.ik.data.target.position;

        if (toTip.magnitude > reach)
            desiredBody = limb.ik.data.target.position
                        + toTip.normalized * reach - offset;
        return desiredBody;
    }

    void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    // ── 아웃라인 ────────────────────────────────────
    void AddOutline(Limb limb)
    {
        if (outlineMaterial == null || limb.renderer == null) return;

        var mats = new List<Material>(limb.renderer.sharedMaterials);
        if (!mats.Contains(outlineMaterial))
        {
            mats.Add(outlineMaterial);
            limb.renderer.sharedMaterials = mats.ToArray();
        }
    }

    void RemoveOutline(Limb limb)
    {
        if (outlineMaterial == null || limb.renderer == null) return;

        var mats = new List<Material>(limb.renderer.sharedMaterials);
        if (mats.Remove(outlineMaterial))
            limb.renderer.sharedMaterials = mats.ToArray();
    }
}