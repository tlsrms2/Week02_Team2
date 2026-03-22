using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class KeyHoldAction : MonoBehaviour
{
    [Header("입력 설정")]
    [Tooltip("꾹 누를 키를 선택하세요")]
    public KeyCode holdKey = KeyCode.Space;
    
    [Tooltip("키를 누르고 있어야 하는 시간 (초)")]
    public float requiredHoldTime = 2.0f;

    [Header("시각적 피드백")]
    [Tooltip("누르는 동안 차오를 UI 이미지 (Image Type이 Filled여야 함)")]
    public Image fillImage;

    [Header("이벤트")]
    [Tooltip("목표 시간을 채웠을 때 실행될 동작을 연결합니다.")]
    public UnityEvent onHoldComplete;

    // 내부 상태 제어 변수
    private float currentHoldTime = 0f;
    private bool isActionTriggered = false; // 중복 실행 방지 플래그

    private void Update()
    {
        // 이벤트가 이미 실행되었다면(스킵 완료), 더 이상 입력을 받지 않음
        if (isActionTriggered) return;

        // 지정된 키를 꾹 누르고 있는 동안
        if (Input.GetKey(holdKey))
        {
            currentHoldTime += Time.deltaTime; // 시간 누적

            // 게이지 UI 업데이트
            if (fillImage != null)
            {
                fillImage.fillAmount = currentHoldTime / requiredHoldTime;
            }

            // 누적 시간이 목표 시간에 도달했을 때
            if (currentHoldTime >= requiredHoldTime)
            {
                isActionTriggered = true;     // 중복 실행 차단
                onHoldComplete?.Invoke();     // 스킵 이벤트 실행
                ResetHold();                  // 게이지 초기화
            }
        }
        else
        {
            // 키에서 손을 떼었을 때 초기화 (누르다 말았을 경우)
            if (currentHoldTime > 0f)
            {
                ResetHold();
            }
        }
    }

    // 시간과 UI를 초기 상태로 되돌리는 캡슐화된 함수
    private void ResetHold()
    {
        currentHoldTime = 0f;
        
        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
        }
    }
}