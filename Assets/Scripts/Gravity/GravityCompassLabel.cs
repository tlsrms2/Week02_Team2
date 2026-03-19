using UnityEngine;
using TMPro;

public class GravityCompassLabel : MonoBehaviour
{
    [Header("참조")]
    public Camera targetCamera;
    public Transform compass3DTransform;   // GravityCompass3D의 Transform

    [Header("UI")]
    public RectTransform labelRoot;        // LabelRoot의 RectTransform
    public TextMeshProUGUI directionLabel;
    public TextMeshProUGUI strengthLabel;
    public CanvasGroup canvasGroup;

    [Header("오프셋")]
    [Tooltip("나침반 3D 위치 기준 아래로 내릴 픽셀")]
    public float offsetY = -80f;
    public float offsetX = 0f;

    [Header("페이드")]
    public float fadeSpeed = 3f;

    private Canvas _canvas;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        _canvas = GetComponentInParent<Canvas>();
    }

    void Update()
    {
        if (GravitySystem.Instance == null) return;

        Vector3 gravity = GravitySystem.Instance.CurrentGravity;
        bool isActive = gravity.sqrMagnitude > 0.01f;

        // 페이드
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(
                canvasGroup.alpha,
                isActive ? 1f : 0f,
                Time.deltaTime * fadeSpeed);
        }

        // ── 3D 위치 → 화면 좌표 변환 ─────────────
        UpdateLabelPosition();

        if (!isActive) return;

        UpdateLabelTexts(gravity);
    }

    void UpdateLabelPosition()
    {
        if (compass3DTransform == null || targetCamera == null || labelRoot == null) return;

        // 3D 월드 위치 → 스크린 좌표
        Vector3 screenPos = targetCamera.WorldToScreenPoint(compass3DTransform.position);

        // 화면 밖이면 숨김
        if (screenPos.z < 0)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            return;
        }

        // Canvas Scaler 보정
        if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // 스크린 좌표 → Canvas 로컬 좌표 변환
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                screenPos,
                null,   // Overlay 모드는 카메라 null
                out Vector2 localPoint);

            labelRoot.localPosition = new Vector3(
                localPoint.x + offsetX,
                localPoint.y + offsetY,
                0f);
        }
    }

    void UpdateLabelTexts(Vector3 gravity)
    {
        Vector3 dir = gravity.normalized;

        // ── Direction + Depth 통합 ───────────────────
        if (directionLabel != null && targetCamera != null)
        {
            float z = Vector3.Dot(dir, targetCamera.transform.forward);

            string dirName = GetDirectionName(dir);
            string depthStr = z > 0.3f ? " ▲" :
                              z < -0.3f ? " ▼" : "";

            directionLabel.text = dirName + depthStr;

            // Z축 성분에 따라 색상 변화
            directionLabel.color = z > 0.3f ? new Color(0.5f, 1f, 0.5f) :  // 앞 → 연두
                                   z < -0.3f ? new Color(1f, 0.5f, 0.5f) :  // 뒤 → 분홍
                                               Color.white;                    // 일반 → 흰색
        }

        if (strengthLabel != null)
            strengthLabel.text = $"{gravity.magnitude:F1} m/s²";
    }

    string GetDirectionName(Vector3 dir)
    {
        (Vector3 vec, string name)[] directions =
        {
            (Vector3.down,    "↓  DOWN"),
            (Vector3.up,      "↑  UP"),
            (Vector3.left,    "←  LEFT"),
            (Vector3.right,   "→  RIGHT"),
            (Vector3.forward, "●  FORWARD"),
            (Vector3.back,    "●  BACK"),
        };

        string best = "–";
        float maxDot = -1f;
        foreach (var (vec, name) in directions)
        {
            float dot = Vector3.Dot(dir, vec);
            if (dot > maxDot) { maxDot = dot; best = name; }
        }
        return best;
    }
}