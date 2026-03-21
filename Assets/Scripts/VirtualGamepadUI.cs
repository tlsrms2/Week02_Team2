using UnityEngine;
using UnityEngine.InputSystem;

public class VirtualGamepadUI : MonoBehaviour
{
    [Header("조이스틱 설정")]
    [Tooltip("움직일 조이스틱 손잡이 이미지 (RectTransform)")]
    public RectTransform joystickKnob;
    [Tooltip("조이스틱이 UI에서 시각적으로 움직일 최대 반경 (픽셀)")]
    public float joystickMaxRadius = 20f;
    private Vector2 joystickOrigin;

    [Header("버튼 UI 매핑")]
    public VirtualButtonUI btnLeftArm_X;   // 왼손 (X 버튼)
    public VirtualButtonUI btnRightArm_Y;  // 오른손 (Y 버튼)
    public VirtualButtonUI btnLeftLeg_A;   // 왼발 (A 버튼)
    public VirtualButtonUI btnRightLeg_B;  // 오른발 (B 버튼)

    [Header("피드백 설정 (인스펙터 조정 가능)")]
    [Range(0f, 1f)]
    [Tooltip("눌렸을 때의 밝기 (1 = 원본 유지, 0 = 완전 검정)")]
    public float dimAmount = 0.5f;
    
    [Tooltip("전환 속도 (클수록 팍팍 즉각적, 작을수록 스르륵 부드러움)")]
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
        var gp = Gamepad.current;
        if (gp == null) return;

        UpdateJoystick(gp);
        UpdateButtonStates(gp);
    }

    private void UpdateJoystick(Gamepad gp)
    {
        if (joystickKnob == null) return;

        Vector2 stickInput = gp.leftStick.ReadValue();
        Vector2 targetPos = joystickOrigin + (stickInput * joystickMaxRadius);
        
        joystickKnob.anchoredPosition = Vector2.Lerp(joystickKnob.anchoredPosition, targetPos, Time.deltaTime * animationSpeed);
    }

    private void UpdateButtonStates(Gamepad gp)
    {
        bool leftArmPressed = gp.leftShoulder.isPressed || gp.buttonWest.isPressed;
        bool rightArmPressed = gp.rightShoulder.isPressed || gp.buttonNorth.isPressed;
        bool leftLegPressed = gp.leftTrigger.ReadValue() > 0.1f || gp.buttonSouth.isPressed;
        bool rightLegPressed = gp.rightTrigger.ReadValue() > 0.1f || gp.buttonEast.isPressed;

        btnLeftArm_X.UpdateVisuals(leftArmPressed, dimAmount, animationSpeed);
        btnRightArm_Y.UpdateVisuals(rightArmPressed, dimAmount, animationSpeed);
        btnLeftLeg_A.UpdateVisuals(leftLegPressed, dimAmount, animationSpeed);
        btnRightLeg_B.UpdateVisuals(rightLegPressed, dimAmount, animationSpeed);
    }
}