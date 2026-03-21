using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // ── 사지 데이터 ───────────────────────────────
    [System.Serializable]
    public class Limb
    {
        public TwoBoneIKConstraint ik;
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
    public Limb leftArm = new() { isLeg = false };
    public Limb rightArm = new() { isLeg = false };
    public Limb leftLeg = new() { isLeg = true };
    public Limb rightLeg = new() { isLeg = true };

    [Header("Input Actions — 키보드 / 마우스")]
    public InputActionReference selectBodyAction;   // Q, E, A, D
    public InputActionReference grabAction;         // Mouse Left Button
    public InputActionReference rotateAction;       // Mouse Delta + Gamepad LeftStick

    [Header("Input Actions — 게임패드 범퍼 (이벤트 방식)")]
    public InputActionReference padLeftArmAction;   // LB
    public InputActionReference padRightArmAction;  // RB

    [Header("게임패드 트리거 (폴링 방식)")]
    [Range(0.1f, 0.9f)]
    public float triggerPressThreshold = 0.3f;
    [Range(0.05f, 0.5f)]
    public float triggerReleaseThreshold = 0.15f;
    [Header("진동")]
    public GamepadHaptics haptics;

    [Header("조작")]
    public float mouseSensitivity = 0.05f;
    public float stickSensitivity = 5f;
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
    public float slideHoldDetectRadius = 0.4f;
    public GameObject leftHandEffect;
    public GameObject rightHandEffect;

    // ── 내부 상태 ─────────────────────────────────
    private Limb[] limbs;
    private Limb activeLimb;
    private Vector3 surfaceNormal = Vector3.back;
    private Vector3 prevBodyPos;

    private LayerMask combinedLayer;
    private bool slideActive;
    private float slideForceTimer;
    private float slideBlinkTimer;
    private bool prevCanGrab = true;
    private float stunTimer;
    private float blinkTimer;

    private Dictionary<string, Limb> limbByKey;
    private Vector2 moveInput;
    private bool inputBlocked;
    private bool usingGamepad;

    // 트리거 폴링 상태
    private bool leftArmHeld;
    private bool rightArmHeld;
    private bool leftTriggerHeld;
    private bool rightTriggerHeld;

    // 외부 차단 플래그 별도 분리
    private bool _externalBlocked = false;

    // ───────────────────────────────────────────────
    void Awake()
    {
        limbs = new[] { leftArm, rightArm, leftLeg, rightLeg };
        combinedLayer = wallLayer | holdLayer;

        limbByKey = new Dictionary<string, Limb>
        {
            { "q", leftArm  },
            { "e", rightArm },
            { "a", leftLeg  },
            { "d", rightLeg },
        };
    }

    void Start()
    {
        GetComponentInParent<RigBuilder>()?.Build();

        foreach (var limb in limbs) InitGrab(limb);

        prevBodyPos = body.position;
        SetCursor(true);
    }

    public void Init()
    {
        foreach (var limb in limbs) InitGrab(limb);

        prevBodyPos = body.position;
        SetCursor(true);
    }

    // ── 이벤트 등록 / 해제 ─────────────────────────
    void OnEnable()
    {
        EnableAction(selectBodyAction, OnSelectBodyKeyboard);
        EnableAction(grabAction, OnGrabMouse);

        if (rotateAction?.action != null)
            rotateAction.action.Enable();

        if (padLeftArmAction?.action != null)
        {
            padLeftArmAction.action.Enable();
            padLeftArmAction.action.started += ctx => OnPadLimbStarted(leftArm);
            padLeftArmAction.action.canceled += ctx => OnPadLimbReleased(leftArm);
        }
        if (padRightArmAction?.action != null)
        {
            padRightArmAction.action.Enable();
            padRightArmAction.action.started += ctx => OnPadLimbStarted(rightArm);
            padRightArmAction.action.canceled += ctx => OnPadLimbReleased(rightArm);
        }
    }

    void OnDisable()
    {
        DisableAction(selectBodyAction, OnSelectBodyKeyboard);
        DisableAction(grabAction, OnGrabMouse);

        if (rotateAction?.action != null)
            rotateAction.action.Disable();

        if (padLeftArmAction?.action != null)
            padLeftArmAction.action.Disable();
        if (padRightArmAction?.action != null)
            padRightArmAction.action.Disable();
    }

    void EnableAction(InputActionReference r,
                      System.Action<InputAction.CallbackContext> cb)
    {
        if (r?.action == null) return;
        r.action.Enable();
        r.action.performed += cb;
    }

    void DisableAction(InputActionReference r,
                       System.Action<InputAction.CallbackContext> cb)
    {
        if (r?.action == null) return;
        r.action.performed -= cb;
        r.action.Disable();
    }

    // ── 키보드 콜백 ───────────────────────────────
    void OnSelectBodyKeyboard(InputAction.CallbackContext ctx)
    {
        if (inputBlocked) return;

        string keyName = ctx.control.name.ToLower();
        if (!limbByKey.TryGetValue(keyName, out var limb)) return;

        usingGamepad = false;
        SelectLimb(limb);
    }

    void OnGrabMouse(InputAction.CallbackContext ctx)
    {
        if (inputBlocked) return;
        TryGrab();
    }

    // ── 범퍼 콜백 (LB/RB) ────────────────────────
    void OnPadLimbStarted(Limb limb)
    {
        if (inputBlocked) return;
        usingGamepad = true;
        SelectLimb(limb);
    }

    void OnPadLimbReleased(Limb limb)
    {
        if (inputBlocked) return;
        if (activeLimb != limb) return;

        // 그랩 시도 → 실패 시 원래 위치로 복귀
        if (!TryGrab())
            RestoreLimb(limb);
    }

    // ── 트리거 폴링 (LT/RT) ────────────────────── 이전꺼 일단 주석처리 해놓음.
    /* void PollTriggers()
    {
        var gp = Gamepad.current;
        if (gp == null) return;

        // ── 왼발 (LT 아날로그 또는 A버튼(South)) ──
        float lt = gp.leftTrigger.ReadValue();
        // 트리거를 설정값 이상 당겼거나(OR), A버튼을 눌렀을 때 true
        bool isLeftLegPressed = lt >= triggerPressThreshold || gp.buttonSouth.isPressed;
        // 트리거를 완전히 뗐고(AND), A버튼도 뗐을 때 true
        bool isLeftLegReleased = lt <= triggerReleaseThreshold && !gp.buttonSouth.isPressed;

        if (!leftTriggerHeld && isLeftLegPressed)
        {
            leftTriggerHeld = true;
            usingGamepad = true;
            if (!inputBlocked) SelectLimb(leftLeg);
        }
        else if (leftTriggerHeld && isLeftLegReleased)
        {
            leftTriggerHeld = false;
            if (!inputBlocked)
            {
                if (activeLimb == leftLeg && !TryGrab())
                    RestoreLimb(leftLeg);
            }
        }

        // ── 오른발 (RT 아날로그 또는 B버튼(East)) ──
        float rt = gp.rightTrigger.ReadValue();
        bool isRightLegPressed = rt >= triggerPressThreshold || gp.buttonEast.isPressed;
        bool isRightLegReleased = rt <= triggerReleaseThreshold && !gp.buttonEast.isPressed;

        if (!rightTriggerHeld && isRightLegPressed)
        {
            rightTriggerHeld = true;
            usingGamepad = true;
            if (!inputBlocked) SelectLimb(rightLeg);
        }
        else if (rightTriggerHeld && isRightLegReleased)
        {
            rightTriggerHeld = false;
            if (!inputBlocked)
            {
                if (activeLimb == rightLeg && !TryGrab())
                    RestoreLimb(rightLeg);
            }
        }
    } */
    
    void PollTriggers()
    {
        var gp = Gamepad.current;
        if (gp == null) return;

        // ── 왼손 (LB 또는 X버튼(West)) ──
        bool isLeftArmPressed = gp.leftShoulder.isPressed || gp.buttonWest.isPressed;
        if (!leftArmHeld && isLeftArmPressed)
        {
            leftArmHeld = true;
            usingGamepad = true;
            if (!inputBlocked) SelectLimb(leftArm);
        }
        else if (leftArmHeld && !isLeftArmPressed)
        {
            leftArmHeld = false;
            if (!inputBlocked)
            {
                if (activeLimb == leftArm && !TryGrab())
                    RestoreLimb(leftArm);
            }
        }

        // ── 오른손 (RB 또는 Y버튼(North)) ──
        bool isRightArmPressed = gp.rightShoulder.isPressed || gp.buttonNorth.isPressed;
        if (!rightArmHeld && isRightArmPressed)
        {
            rightArmHeld = true;
            usingGamepad = true;
            if (!inputBlocked) SelectLimb(rightArm);
        }
        else if (rightArmHeld && !isRightArmPressed)
        {
            rightArmHeld = false;
            if (!inputBlocked)
            {
                if (activeLimb == rightArm && !TryGrab())
                    RestoreLimb(rightArm);
            }
        }

        // ── 왼발 (LT 아날로그 또는 A버튼(South)) ──
        float lt = gp.leftTrigger.ReadValue();
        bool isLeftLegPressed = lt >= triggerPressThreshold || gp.buttonSouth.isPressed;
        if (!leftTriggerHeld && isLeftLegPressed)
        {
            leftTriggerHeld = true;
            usingGamepad = true;
            if (!inputBlocked) SelectLimb(leftLeg);
        }
        else if (leftTriggerHeld && !isLeftLegPressed)
        {
            leftTriggerHeld = false;
            if (!inputBlocked)
            {
                if (activeLimb == leftLeg && !TryGrab())
                    RestoreLimb(leftLeg);
            }
        }

        // ── 오른발 (RT 아날로그 또는 B버튼(East)) ──
        float rt = gp.rightTrigger.ReadValue();
        bool isRightLegPressed = rt >= triggerPressThreshold || gp.buttonEast.isPressed;
        if (!rightTriggerHeld && isRightLegPressed)
        {
            rightTriggerHeld = true;
            usingGamepad = true;
            if (!inputBlocked) SelectLimb(rightLeg);
        }
        else if (rightTriggerHeld && !isRightLegPressed)
        {
            rightTriggerHeld = false;
            if (!inputBlocked)
            {
                if (activeLimb == rightLeg && !TryGrab())
                    RestoreLimb(rightLeg);
            }
        }
    }

    // ── 공통 사지 선택 ────────────────────────────
    void SelectLimb(Limb limb)
    {
        if (activeLimb != null && activeLimb != limb)
        {
            if (usingGamepad)
            {
                // 게임패드: 기존 사지 그랩 시도 → 실패 시 복귀
                if (!TryGrab())
                    RestoreLimb(activeLimb);
            }
            else
            {
                // 키보드: 기존 방식 유지 (클릭으로 그랩)
                RemoveOutline(activeLimb);
                activeLimb.grabbed = true;
                activeLimb.ik.data.target.position = activeLimb.grabPos;
            }
        }

        activeLimb = limb;
        limb.grabbed = false;
        AddOutline(limb);
        SetCursor(true);
    }

    /// <summary>
    /// 그랩 실패 시 사지를 이전 그랩 위치로 되돌리고 활성 상태 해제
    /// </summary>
    void RestoreLimb(Limb limb)
    {
        limb.grabbed = true;
        limb.ik.data.target.position = limb.grabPos;
        RemoveOutline(limb);

        if (activeLimb == limb)
            activeLimb = null;

        SetCursor(true);
    }

    // ───────────────────────────────────────────────
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Slide(4);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Stun(2);
        }
        //inputBlocked = stunTimer > 0f || (slideActive && slideForceTimer > 0f);

        // 외부 차단 OR 스턴/슬라이드 차단
        inputBlocked = _externalBlocked || stunTimer > 0f || (slideActive && slideForceTimer > 0f);


        // 스턴 중 깜빡임
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

        if (slideActive && slideForceTimer > 0f) return;

        // 외부 차단 상태면 이하 입력 처리 스킵
        if (_externalBlocked) return;


        // 트리거 폴링 (매 프레임)
        PollTriggers();

        // 활성 사지 이동
        if (activeLimb != null)
        {
            if (rotateAction?.action != null)
                moveInput = rotateAction.action.ReadValue<Vector2>();
            else
                moveInput = Vector2.zero;

            MoveActiveLimb();
            UpdateOutlineColor();
        }
    }

    void LateUpdate()
    {
        ProcessSlide();

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
        haptics?.PlayStun(time);
        
        // 기절 효과음 재생
        SoundManager.Instance?.PlayStun();
        
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
    public void Slide(float time)
    {
        if (activeLimb != null)
        {
            RemoveOutline(activeLimb);
            activeLimb.grabbed = true;
            activeLimb.ik.data.target.position = activeLimb.grabPos;
            activeLimb = null;
            SetCursor(true);
        }

        slideForceTimer = time;
        slideBlinkTimer = 0f;
        slideActive = true;
        haptics?.PlaySlideStart();
        
        // 슬라이딩 효과음 재생
        SoundManager.Instance?.PlaySlide();

        if (leftHandEffect != null) leftHandEffect.SetActive(true);
        if (rightHandEffect != null) rightHandEffect.SetActive(true);
    }

    void ProcessSlide()
    {
        if (!slideActive) return;

        if (slideForceTimer > 0f)
        {
            slideForceTimer -= Time.deltaTime;

            if (blinkTarget != null)
            {
                slideBlinkTimer -= Time.deltaTime;
                if (slideBlinkTimer <= 0f)
                {
                    blinkTarget.SetActive(!blinkTarget.activeSelf);
                    slideBlinkTimer = blinkInterval;
                }

                if (slideForceTimer <= 0f)
                    blinkTarget.SetActive(true);
            }

            if (slideForceTimer <= 0f)
            {
                if (leftHandEffect != null) leftHandEffect.SetActive(false);
                if (rightHandEffect != null) rightHandEffect.SetActive(false);
            }
        }

        GetSurfaceBasis(out _, out var surfUp);
        Vector3 slideDelta = -surfUp * (slideSpeed * Time.deltaTime);

        bool foundHold = false;
        if (slideForceTimer <= 0f)
        {
            if (leftArm.grabbed
                && Physics.OverlapSphere(leftArm.grabPos, slideHoldDetectRadius, holdLayer).Length > 0)
                foundHold = true;
            if (!foundHold && rightArm.grabbed
                && Physics.OverlapSphere(rightArm.grabPos, slideHoldDetectRadius, holdLayer).Length > 0)
                foundHold = true;
        }

        if (foundHold)
        {
            slideActive = false;
            haptics?.StopSlide();
            return;
        }

        foreach (var limb in limbs)
        {
            if (!limb.grabbed) continue;

            Vector3 newPos = limb.grabPos + slideDelta;

            var ray = new Ray(newPos + surfaceNormal * 1f, -surfaceNormal);
            if (Physics.Raycast(ray, out var hit, 3f, combinedLayer))
                newPos = hit.point + hit.normal * handRadius;

            limb.grabPos = newPos;
        }
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
        float mx, my;

        if (usingGamepad)
        {
            mx = moveInput.x * stickSensitivity * Time.deltaTime;
            my = moveInput.y * stickSensitivity * Time.deltaTime;
        }
        else
        {
            mx = moveInput.x * mouseSensitivity * Time.deltaTime;
            my = moveInput.y * mouseSensitivity * Time.deltaTime;
        }

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
    /// <returns>그랩 성공 여부</returns>
    bool TryGrab()
    {
        if (activeLimb == null) return false;

        var pos = activeLimb.ik.data.target.position;
        var hits = Physics.OverlapSphere(pos, grabRange, holdLayer);
        if (hits.Length == 0) return false;

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

        haptics?.PlayGrab();
        
        // 벽 잡기 성공 효과음 재생 (Climbing 사운드 사용)
        SoundManager.Instance?.PlayClimbing();
        
        return true;
    }

    void InitGrab(Limb limb)
    {
        if (limb.ik == null) return;
        var pos = limb.ik.data.target.position;
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

    // 외부에서 입력 차단 제어
    public void SetInputBlocked(bool blocked)
    {
        inputBlocked = blocked;

        // 차단 시 활성 사지 초기화
        if (blocked && activeLimb != null)
        {
            RemoveOutline(activeLimb);
            activeLimb.grabbed = true;
            activeLimb.ik.data.target.position = activeLimb.grabPos;
            activeLimb = null;
            SetCursor(false); // 커서 해제
        }
    }
}