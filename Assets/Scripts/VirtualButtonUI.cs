using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class VirtualButtonUI
{
    [Tooltip("피드백을 줄 UI 버튼 이미지")]
    public Image buttonImage;
    private Color originalColor;

    public void Initialize()
    {
        if (buttonImage != null) 
            originalColor = buttonImage.color;
    }

    public void UpdateVisuals(bool isPressed, float dimFactor, float lerpSpeed)
    {
        if (buttonImage == null) return;
        
        Color targetColor = originalColor;
        if (isPressed)
        {
            targetColor.r *= dimFactor;
            targetColor.g *= dimFactor;
            targetColor.b *= dimFactor;
        }

        buttonImage.color = Color.Lerp(buttonImage.color, targetColor, Time.deltaTime * lerpSpeed);
    }
}