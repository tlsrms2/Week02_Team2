using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro를 사용하기 위해 반드시 추가

[System.Serializable]
public class VirtualButtonUI
{
    [Tooltip("피드백을 줄 UI 버튼 이미지")]
    public Image buttonImage;

    // ── 새로 추가된 텍스트 관련 변수들 ──
    [Tooltip("버튼 옆에 표시될 텍스트 컴포넌트 (TMP)")]
    public TextMeshProUGUI buttonText;
    
    [Tooltip("키보드 사용 시 표시될 텍스트 (예: Q, E)")]
    public string keyboardLabel;
    
    [Tooltip("패드 사용 시 표시될 텍스트 (예: LB, RB)")]
    public string gamepadLabel;
    // ─────────────────────────────────

    private Color originalColor;

    public void Initialize()
    {
        if (buttonImage != null) 
            originalColor = buttonImage.color;
    }

    public void UpdateVisuals(bool isPressed, float dimFactor, float lerpSpeed)
    {
        // (기존 이미지 투명도 조절 로직 동일)
        if (buttonImage == null) return;
        Color targetColor = originalColor;
        if (isPressed)
        {
            targetColor.r *= dimFactor; targetColor.g *= dimFactor; targetColor.b *= dimFactor;
        }
        buttonImage.color = Color.Lerp(buttonImage.color, targetColor, Time.deltaTime * lerpSpeed);
    }

    // ── 새로 추가된 텍스트 변경 함수 ──
    public void UpdateLabel(bool isGamepad)
    {
        if (buttonText != null)
        {
            buttonText.text = isGamepad ? gamepadLabel : keyboardLabel;
        }
    }
}