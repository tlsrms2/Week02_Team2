using UnityEngine;
using UnityEngine.InputSystem; // New Input System 사용

public class VirtualGamepadUI : MonoBehaviour
{
    [Header("조이스틱 설정")]
    [Tooltip("움직일 조이스틱 손잡이 이미지 (RectTransform)")]
    public RectTransform joystickKnob;
    [Tooltip("조이스틱이 UI에서 시각적으로 움직일 최대 반경 (픽셀)")]
    public float joystickMaxRadius = 20f;
    [Tooltip("마우스 움직임이 조이스틱 UI에 반영되는 민감도")]
    public float mouseUISensitivity = 0.1f; 
    private Vector2 joystickOrigin;
    
    // 추가: 마지막으로 입력된 기기가 게임패드인지 추적하는 변수
    private bool isGamepadLastUsed = false;

    [Header("버튼 UI 매핑")]
    public VirtualButtonUI btnLeftArm_X;   // 왼손 (X 버튼 / Q 키)
    public VirtualButtonUI btnRightArm_Y;  // 오른손 (Y 버튼 / E 키)
    public VirtualButtonUI btnLeftLeg_A;   // 왼발 (A 버튼 / A 키)
    public VirtualButtonUI btnRightLeg_B;  // 오른발 (B 버튼 / D 키)

    [Header("피드백 설정")]
    [Range(0f, 1f)] public float dimAmount = 0.5f;
    public float animationSpeed = 15f;

    void Start()
    {
        if (joystickKnob != null)
            joystickOrigin = joystickKnob.anchoredPosition;

        btnLeftArm_X.Initialize();
        btnRightArm_Y.Initialize();
        btnLeftLeg_A.Initialize();
        btnRightLeg_B.Initialize();
    }

    void Update()
    {
        // 1. 상태를 저장할 임시 변수들
        Vector2 finalStickInput = Vector2.zero;
        bool lArm = false, rArm = false, lLeg = false, rLeg = false;
        
        // 입력을 감지했는지 체크하기 위한 임시 변수
        bool gamepadInputDetected = false;
        bool keyboardMouseInputDetected = false;

        // 2. 게임패드 입력 확인
        var gp = Gamepad.current;
        if (gp != null)
        {
            finalStickInput = gp.leftStick.ReadValue();
            lArm = gp.leftShoulder.isPressed || gp.buttonWest.isPressed;
            rArm = gp.rightShoulder.isPressed || gp.buttonNorth.isPressed;
            lLeg = gp.leftTrigger.ReadValue() > 0.1f || gp.buttonSouth.isPressed;
            rLeg = gp.rightTrigger.ReadValue() > 0.1f || gp.buttonEast.isPressed;
            
            // 패드의 스틱이 움직이거나 버튼이 하나라도 눌렸다면 패드 입력 감지
            if (finalStickInput.sqrMagnitude > 0.01f || lArm || rArm || lLeg || rLeg)
                gamepadInputDetected = true;
        }

        // 3. 키보드 입력 누적
        var kb = Keyboard.current;
        if (kb != null)
        {
            lArm |= kb.qKey.isPressed;
            rArm |= kb.eKey.isPressed;
            lLeg |= kb.aKey.isPressed;
            rLeg |= kb.dKey.isPressed;

            // 아무 키보드 버튼이나 눌렸다면 키마 입력 감지
            if (kb.anyKey.wasPressedThisFrame) 
                keyboardMouseInputDetected = true;
        }

        // 4. 마우스 입력 확인
        var ms = Mouse.current;
        if (ms != null)
        {
            // 마우스 이동량(Delta)을 조이스틱의 X, Y 입력으로 변환
            Vector2 mouseDelta = ms.delta.ReadValue();
            if (mouseDelta.sqrMagnitude > 0.01f) // 마우스가 조금이라도 움직였다면
            {
                // 값이 너무 커서 UI를 뚫고 나가지 않도록 ClampMagnitude로 최대 길이를 1로 제한
                finalStickInput = Vector2.ClampMagnitude(mouseDelta * mouseUISensitivity, 1f);
                keyboardMouseInputDetected = true; // 마우스 입력 감지 추가
            }
        }

        // 5. 최근 사용 기기 상태 갱신
        if (gamepadInputDetected) isGamepadLastUsed = true;
        else if (keyboardMouseInputDetected) isGamepadLastUsed = false;

        // 6. 취합된 최종 결과로 UI 시각적 업데이트
        UpdateJoystickUI(finalStickInput);
        UpdateButtonStatesUI(lArm, rArm, lLeg, rLeg);

        // 7. 판단된 기기 상태에 따라 텍스트 라벨 변경 지시
        btnLeftArm_X.UpdateLabel(isGamepadLastUsed);
        btnRightArm_Y.UpdateLabel(isGamepadLastUsed);
        btnLeftLeg_A.UpdateLabel(isGamepadLastUsed);
        btnRightLeg_B.UpdateLabel(isGamepadLastUsed);
    }

    private void UpdateJoystickUI(Vector2 input)
    {
        if (joystickKnob == null) return;

        Vector2 targetPos = joystickOrigin + (input * joystickMaxRadius);
        joystickKnob.anchoredPosition = Vector2.Lerp(joystickKnob.anchoredPosition, targetPos, Time.deltaTime * animationSpeed);
    }

    private void UpdateButtonStatesUI(bool leftArm, bool rightArm, bool leftLeg, bool rightLeg)
    {
        btnLeftArm_X.UpdateVisuals(leftArm, dimAmount, animationSpeed);
        btnRightArm_Y.UpdateVisuals(rightArm, dimAmount, animationSpeed);
        btnLeftLeg_A.UpdateVisuals(leftLeg, dimAmount, animationSpeed);
        btnRightLeg_B.UpdateVisuals(rightLeg, dimAmount, animationSpeed);
    }
}