using UnityEngine;
using UnityEngine.EventSystems;

// IPointerEnterHandler: 마우스가 버튼 영역에 들어왔을 때 감지
// IPointerExitHandler: 마우스가 버튼 영역에서 나갔을 때 감지
public class RetroButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("레트로 시각 설정")]
    [Tooltip("마우스가 올라갔을 때 즉시 커질 배율")]
    public float selectScale = 1.2f;
    
    [Tooltip("화면 캔버스에 하나 존재하는 포인터(▶) 이미지")]
    public RectTransform pointerUI;
    
    [Tooltip("버튼 중앙을 기준으로 포인터가 위치할 오프셋")]
    public Vector3 pointerOffset = new Vector3(-150f, 0f, 0f);

    private Vector3 originalScale;

    private void Awake()
    {
        // 시작할 때 자신의 원래 크기를 기억해 둡니다.
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 1. 1프레임만에 즉시 크기를 키웁니다.
        transform.localScale = originalScale * selectScale;

        // 2. 포인터(▶)를 활성화하고, 현재 버튼의 위치 + 오프셋 위치로 즉시 이동시킵니다.
        if (pointerUI != null)
        {
            pointerUI.gameObject.SetActive(true);
            pointerUI.position = transform.position + pointerOffset;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 1. 즉시 원래 크기로 복구합니다.
        transform.localScale = originalScale;

        // 2. 마우스가 벗어났으므로 포인터(▶)를 다시 숨깁니다.
        if (pointerUI != null)
        {
            pointerUI.gameObject.SetActive(false);
        }
    }
}