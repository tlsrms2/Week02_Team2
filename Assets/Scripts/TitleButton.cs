using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TitleButton : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Outline outline;          // Button Image에 붙은 Outline 컴포넌트
    [SerializeField] private TextMeshProUGUI arrow;    // "▶" 화살표 TMP

    [Header("클릭 이벤트")]
    [SerializeField] public UnityEvent onClickEvent;

    private RectTransform _rect;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    public void SetHighlight(bool on, Color color)
    {
        // 텍스트 색상
        label.color = color;

        // 아웃라인
        if (outline != null)
            outline.enabled = on;

        // 화살표
        if (arrow != null)
            arrow.gameObject.SetActive(on);
    }

    public bool IsMouseOver()
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            _rect,
            Input.mousePosition,
            Camera.main
        );
    }

    public void OnClick()
    {
        onClickEvent?.Invoke();
    }
}