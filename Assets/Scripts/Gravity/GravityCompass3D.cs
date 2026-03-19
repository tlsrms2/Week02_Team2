using System.Collections.Generic;
using UnityEngine;

public class GravityCompass3D : MonoBehaviour
{
    [Header("참조")]
    public Camera targetCamera;
    public Transform arrowPivot;
    public Transform arrowHead;
    public Transform arrowTail;
    public Transform sphereVisual;

    [Header("크기")]
    public float compassScale = 0.35f;
    public float arrowLength = 0.7f;

    [Header("화면 위치")]
    public Vector2 viewportPosition = new Vector2(0.08f, 0.12f);
    public float distanceFromCamera = 3f;

    [Header("애니메이션")]
    public float rotationSpeed = 8f;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.04f;

    [Header("색상")]
    public Color sphereColor = new Color(0.1f, 0.15f, 0.3f, 0.7f);
    public Color arrowColorActive = new Color(1f, 0.85f, 0.1f, 1f);
    public Color arrowColorOff = new Color(0.4f, 0.4f, 0.4f, 0.4f);

    [Header("페이드")]
    public float fadeSpeed = 3f;
    public float minAlpha = 0f;
    public float maxAlpha = 1f;

    [Header("페이드 대상 (6개)")]
    public Renderer sphereRenderer;
    public Renderer ringXRenderer;
    public Renderer ringYRenderer;
    public Renderer ringZRenderer;
    public Renderer arrowTailRenderer;
    public Renderer arrowHeadRenderer;

    // ── 내부 상태 ──────────────────────────────────
    private Quaternion _targetRotation;
    private float _pulseTimer = 0f;
    private bool _isActive = false;
    private float _currentAlpha = 0f;

    private struct RendererInfo
    {
        public Renderer renderer;
        public Material material;   // ← mpb 대신 Material 인스턴스
        public Color baseColor;
    }
    private List<RendererInfo> _rendererInfos = new List<RendererInfo>();

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        _rendererInfos.Clear();

        // sphereColor는 이미 알파 0.7 설정되어 있음
        RegisterRenderer(sphereRenderer, sphereColor);

        // Ring은 sharedMaterial 색상 그대로 (이미 알파 0.31)
        RegisterRenderer(ringXRenderer, GetMatColor(ringXRenderer));
        RegisterRenderer(ringYRenderer, GetMatColor(ringYRenderer));
        RegisterRenderer(ringZRenderer, GetMatColor(ringZRenderer));

        // ← 화살표는 알파를 1.0 → 0.8로 명시 (페이드가 눈에 보이도록)
        Color arrowBase = arrowColorActive;
        arrowBase.a = 0.8f;
        RegisterRenderer(arrowTailRenderer, arrowBase);
        RegisterRenderer(arrowHeadRenderer, arrowBase);

        transform.localScale = Vector3.one * compassScale;
    }

    void RegisterRenderer(Renderer rend, Color baseColor)
    {
        if (rend == null) return;

        // sharedMaterial을 기준으로 새 인스턴스 생성
        // rend.material로 읽으면 이미 인스턴스화된 것을 읽을 수 있으므로
        // 반드시 sharedMaterial 기준으로 새로 생성
        Material original = rend.sharedMaterial;
        Material matInstance = new Material(original);

        // 즉시 적용
        rend.sharedMaterial = matInstance;

        _rendererInfos.Add(new RendererInfo
        {
            renderer = rend,
            material = matInstance,
            baseColor = baseColor
        });
    }

    Color GetMatColor(Renderer rend)
    {
        return rend != null && rend.sharedMaterial != null
            ? rend.sharedMaterial.color
            : Color.white;
    }

    void LateUpdate()
    {
        FollowCamera();
        UpdateArrow();
        UpdateVisuals();
        UpdateFade();
    }

    void UpdateFade()
    {
        float targetAlpha = _isActive ? maxAlpha : minAlpha;

        _currentAlpha = Mathf.Lerp(
            _currentAlpha, targetAlpha,
            Time.deltaTime * fadeSpeed);

        // 알파가 거의 0이면 오브젝트 끄기
        bool shouldShow = _currentAlpha > 0.01f;

        foreach (RendererInfo info in _rendererInfos)
        {
            if (info.renderer == null) continue;

            // 오브젝트 활성/비활성
            info.renderer.gameObject.SetActive(shouldShow);

            if (!shouldShow) continue;

            // 알파 적용
            Color c = info.baseColor;
            c.a = c.a * _currentAlpha;

            info.material.SetColor("_BaseColor", c);
            info.material.SetColor("_Color", c);
        }
    }

    void FollowCamera()
    {
        if (targetCamera == null) return;
        Vector3 viewPos = new Vector3(
            viewportPosition.x,
            viewportPosition.y,
            distanceFromCamera);
        transform.position = targetCamera.ViewportToWorldPoint(viewPos);
        transform.rotation = targetCamera.transform.rotation;
    }

    void UpdateArrow()
    {
        if (GravitySystem.Instance == null || arrowPivot == null) return;

        Vector3 gravity = GravitySystem.Instance.CurrentGravity;
        _isActive = gravity.sqrMagnitude > 0.01f;

        if (!_isActive) return;

        _targetRotation = Quaternion.LookRotation(gravity.normalized);
        arrowPivot.rotation = Quaternion.Slerp(
            arrowPivot.rotation,
            _targetRotation,
            Time.deltaTime * rotationSpeed);
    }

    void UpdateVisuals()
    {
        _pulseTimer += Time.deltaTime * pulseSpeed;

        if (sphereVisual != null)
        {
            float pulse = _isActive
                ? 1f + Mathf.Sin(_pulseTimer) * pulseAmount
                : 1f;
            sphereVisual.localScale = Vector3.one * pulse;
        }

        for (int i = 0; i < _rendererInfos.Count; i++)
        {
            RendererInfo info = _rendererInfos[i];
            if (info.renderer == arrowTailRenderer ||
                info.renderer == arrowHeadRenderer)
            {
                RendererInfo updated = info;
                updated.baseColor = _isActive ? arrowColorActive : arrowColorOff;
                _rendererInfos[i] = updated;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (targetCamera == null) return;
        Gizmos.color = Color.cyan;
        Vector3 viewPos = new Vector3(
            viewportPosition.x,
            viewportPosition.y,
            distanceFromCamera);
        Gizmos.DrawWireSphere(targetCamera.ViewportToWorldPoint(viewPos), 0.05f);
    }
}