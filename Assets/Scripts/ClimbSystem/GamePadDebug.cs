using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadDebug : MonoBehaviour
{
    void Start()
    {
        // 현재 연결된 모든 디바이스 출력
        foreach (var device in InputSystem.devices)
            Debug.Log($"[Device] {device.displayName} ({device.deviceId})");

        // 게임패드 연결/해제 감지
        InputSystem.onDeviceChange += (device, change) =>
        {
            if (device is Gamepad)
                Debug.Log($"[Gamepad] {device.displayName} → {change}");
        };
    }

    void Update()
    {
        var gp = Gamepad.current;
        if (gp == null) return;

        // 아무 버튼이든 눌리면 control 이름 출력
        foreach (var control in gp.allControls)
        {
            if (control is UnityEngine.InputSystem.Controls.ButtonControl btn
                && btn.wasPressedThisFrame)
            {
                Debug.Log($"[Pressed] {control.name} ({control.displayName})");
            }
        }

        // 스틱 값 확인
        var stick = gp.leftStick.ReadValue();
        if (stick.magnitude > 0.1f)
            Debug.Log($"[LeftStick] x:{stick.x:F2} y:{stick.y:F2}");
    }
}