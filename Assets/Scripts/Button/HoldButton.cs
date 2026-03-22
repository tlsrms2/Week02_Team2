using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// IPointerDownHandler, IPointerUpHandler 인터페이스를 상속받아 UI 클릭/터치 이벤트를 감지합니다.
public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("설정")]
    [Tooltip("버튼을 누르고 있어야 하는 시간 (초)")]
    public float requiredHoldTime = 2.0f;

    [Header("시각적 피드백 (선택 사항)")]
    [Tooltip("누르는 동안 차오를 UI 이미지 (Image Type이 Filled여야 함)")]
    public Image fillImage;

    [Header("이벤트")]
    [Tooltip("목표 시간을 채웠을 때 실행될 동작을 에디터에서 연결합니다.")]
    public UnityEvent onLongPressComplete;

    // 캡슐화된 내부 상태 변수들
    private bool isPointerDown = false;
    private float currentHoldTime = 0f;
    private bool isActionTriggered = false; // 중복 실행 방지용 플래그

    // 마우스나 터치로 버튼을 누르기 시작할 때 호출됨
    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        isActionTriggered = false; // 누를 때마다 상태 초기화
    }

    // 마우스나 터치로 버튼에서 손을 뗄 때 호출됨
    public void OnPointerUp(PointerEventData eventData)
    {
        ResetButton();
    }

    private void Update()
    {
        // 버튼을 누르고 있고, 아직 이벤트가 실행되지 않은 상태일 때만 시간 계산
        if (isPointerDown && !isActionTriggered)
        {
            currentHoldTime += Time.deltaTime; // 매 프레임마다 누적 시간 증가

            // 시각적 피드백 업데이트 (게이지 채우기)
            if (fillImage != null)
            {
                // currentHoldTime / requiredHoldTime 은 0.0 ~ 1.0 사이의 비율을 반환합니다.
                fillImage.fillAmount = currentHoldTime / requiredHoldTime;
            }

            // 누적 시간이 목표 시간에 도달했는지 확인
            if (currentHoldTime >= requiredHoldTime)
            {
                isActionTriggered = true;     // 중복 실행 방지를 위해 true로 변경
                onLongPressComplete?.Invoke(); // 유니티 에디터에 연결된 이벤트(튜토리얼 스킵 등) 실행
                ResetButton();                // 완료 후 상태 초기화
            }
        }
    }

    // 버튼의 상태와 게이지 UI를 초기 상태로 되돌리는 함수
    private void ResetButton()
    {
        isPointerDown = false;
        currentHoldTime = 0f;
        
        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
        }
    }
}