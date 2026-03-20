using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerController : MonoBehaviour
{
    // ── 사지 데이터 ───────────────────────────────
    [System.Serializable]
    public class Limb
    {
        public TwoBoneIKConstraint ik;
        public KeyCode key;
        public bool isLeg;
        public Renderer renderer;

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
    public Material outlineInvalidMaterial;

    [Header("피격 깜빡임")]
    public GameObject blinkTarget;
    public float blinkInterval = 0.1f;

    [Header("슬라이드")]
    public float slideSpeed = 3f;
    public float slideDamping = 5f;
    public float slideDriftSpeed = 1.5f;
    public float slideHoldDetectRadius = 0.4f;

    // ── 내부 상태 ─────────────────────────────────
    private Limb[] limbs;
    private Limb activeLimb;
    private Vector3 surfaceNormal = Vector3.back;
    private Vector3 prevBodyPos;

    private LayerMask combinedLayer;
    private float slideVelocity;
    private bool slideDrifting;
    private float slideForceTimer;
    private bool prevCanGrab = true;
    private float stunTimer;
    private float blinkTimer;

    // ───────────────────────────────────────────────
    void Start()
    {
        limbs = new[] { leftArm, rightArm, leftLeg, rightLeg };
        combinedLayer = wallLayer | holdLayer;

        GetComponentInParent<RigBuilder>()?.Build();

        foreach (var limb in limbs) InitGrab(limb);

        prevBodyPos = body.position;
        SetCursor(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Slide(7, 1);
        }

        // 스턴 중 입력 차단 + 깜빡임
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;

            if (blinkTarget != null)
            {
                blinkTimer -= Time.deltaTime;
                if (blinkTimer <= 0f)
                {
                    blinkTarget.SetActive(!blinkTarget.activeSelf);
                    blinkTimer = blinkInterval;
                }

                if (stunTimer <= 0f)
                    blinkTarget.SetActive(true);
            }

            return;
        }

        // 사지 선택
        foreach (var limb in limbs)
        {
            if (Input.GetKeyDown(limb.key))
            {
                if (activeLimb != null)
                {
                    RemoveOutline(activeLimb);
                    activeLimb.grabbed = true;
                    activeLimb.ik.data.target.position = activeLimb.grabPos;
                }

                activeLimb = limb;
                limb.grabbed = false;
                AddOutline(limb);
                SetCursor(true);
            }
        }

        // 활성 사지 마우스 이동
        if (activeLimb != null)
        {
            MoveActiveLimb();
            UpdateOutlineColor();
        }

        // 클릭으로 그랩
        if (Input.GetMouseButtonDown(0)) TryGrab();
    }

    void LateUpdate()
    {
        ProcessSlide();

        // 그랩된 타겟 고정
        foreach (var limb in limbs)
            if (limb.grabbed)
                limb.ik.data.target.position = limb.grabPos;

        UpdateSurfaceNormal();
        UpdateFreeLimbs();
        UpdateBody();
        UpdateRotation();
    }

    // ── 스턴 ──────────────────────────────────────
    public void Stun(float time)
    {
        stunTimer = Mathf.Max(stunTimer, time);
        blinkTimer = 0f;

        if (activeLimb != null)
        {
            RemoveOutline(activeLimb);
            activeLimb.grabbed = true;
            activeLimb.ik.data.target.position = activeLimb.grabPos;
            activeLimb = null;
            SetCursor(true);
        }
    }

    // ── 슬라이드 ────────────────────────────────────
    /// <summary>
    /// 외부에서 호출. time초 동안 무조건 미끄러진 뒤,
    /// 홀드를 만날 때까지 계속 미끄러집니다.
    /// </summary>
    public void Slide(float distance, float time)
    {
        slideVelocity += distance * slideSpeed;
        slideForceTimer = time;
        slideDrifting = true;
    }

    void ProcessSlide()
    {
        if (!slideDrifting && Mathf.Abs(slideVelocity) < 0.001f) return;

        if (slideForceTimer > 0f)
            slideForceTimer -= Time.deltaTime;

        GetSurfaceBasis(out _, out var surfUp);
        float step = slideVelocity * Time.deltaTime;
        Vector3 slideDelta = -surfUp * step;

        bool foundHold = false;
        foreach (var limb in limbs)
        {
            if (!limb.grabbed) continue;

            Vector3 newPos = limb.grabPos + slideDelta;

            var ray = new Ray(newPos + surfaceNormal * 1f, -surfaceNormal);
            if (Physics.Raycast(ray, out var hit, 3f, combinedLayer))
                newPos = hit.point + hit.normal * handRadius;

            if (slideForceTimer <= 0f
                && Physics.OverlapSphere(newPos, slideHoldDetectRadius, holdLayer).Length > 0)
                foundHold = true;

            limb.grabPos = newPos;
        }

        if (foundHold)
        {
            slideVelocity = 0f;
            slideDrifting = false;
            return;
        }

        slideVelocity = Mathf.Lerp(slideVelocity, 0f,
                                    Time.deltaTime * slideDamping);

        if (slideDrifting && Mathf.Abs(slideVelocity) < slideDriftSpeed)
            slideVelocity = slideDriftSpeed;
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

        var normal = GetNormalAt(pos);
        var worldRef = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.9f
            ? Vector3.forward : Vector3.up;
        var right = Vector3.Cross(normal, worldRef).normalized;
        var up = Vector3.Cross(right, normal).normalized;

        pos += right * mx + up * my;
        pos = SnapToSurface(pos, GetNormalAt(pos), target.position);
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

        bool canSnap = col is BoxCollider or SphereCollider or CapsuleCollider
                    || (col is MeshCollider mc && mc.convex);
        if (canSnap)
        {
            var contact = Physics.ClosestPoint(pos, col,
                              col.transform.position, col.transform.rotation);
            pos = contact + (pos - contact).normalized * handRadius;
        }

        var ray = new Ray(pos + surfaceNormal * 0.5f, -surfaceNormal);
        activeLimb.grabNormal = Physics.Raycast(ray, out var hit, 2f, combinedLayer)
            ? hit.normal : surfaceNormal;

        activeLimb.grabPos = pos;
        activeLimb.grabbed = true;
        activeLimb.ik.data.target.position = pos;
        RemoveOutline(activeLimb);
        activeLimb = null;
        SetCursor(true);
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

        var center = Vector3.zero;
        foreach (var p in anchors) center += p;
        center /= anchors.Count;

        float targetX = center.x;
        if (activeLimb != null)
            targetX = Mathf.Lerp(center.x,
                      activeLimb.ik.data.target.position.x,
                      bodyFollowWeight);

        float targetY = ComputeBodyY();

        var desired = new Vector3(targetX, targetY, center.z);
        desired = ApplyWallStandoff(desired);

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

        if (activeLimb != null && !activeLimb.isLeg)
        {
            float activeY = activeLimb.ik.data.target.position.y;
            baseY = Mathf.Lerp(baseY, activeY - naturalHangLength, 0.85f);
        }

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
    Material CurrentOutline => prevCanGrab ? outlineMaterial : outlineInvalidMaterial;

    void AddOutline(Limb limb)
    {
        if (limb.renderer == null) return;
        var mat = CurrentOutline;
        if (mat == null) return;

        var mats = new List<Material>(limb.renderer.sharedMaterials);
        if (!mats.Contains(mat))
        {
            mats.Add(mat);
            limb.renderer.sharedMaterials = mats.ToArray();
        }
    }

    void RemoveOutline(Limb limb)
    {
        if (limb.renderer == null) return;

        var mats = new List<Material>(limb.renderer.sharedMaterials);
        bool changed = false;
        if (outlineMaterial != null) changed |= mats.Remove(outlineMaterial);
        if (outlineInvalidMaterial != null) changed |= mats.Remove(outlineInvalidMaterial);
        if (changed) limb.renderer.sharedMaterials = mats.ToArray();
    }

    void UpdateOutlineColor()
    {
        if (activeLimb?.renderer == null) return;

        var pos = activeLimb.ik.data.target.position;
        bool canGrab = Physics.OverlapSphere(pos, grabRange, holdLayer).Length > 0;

        if (canGrab == prevCanGrab) return;
        prevCanGrab = canGrab;

        var oldMat = canGrab ? outlineInvalidMaterial : outlineMaterial;
        var newMat = canGrab ? outlineMaterial : outlineInvalidMaterial;
        if (oldMat == null || newMat == null) return;

        var mats = new List<Material>(activeLimb.renderer.sharedMaterials);
        int idx = mats.IndexOf(oldMat);
        if (idx >= 0)
            mats[idx] = newMat;
        else
            mats.Add(newMat);
        activeLimb.renderer.sharedMaterials = mats.ToArray();
    }
}