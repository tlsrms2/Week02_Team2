using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ControllerSetupUI : MonoBehaviour
{
    [Header("연결 참조")]
    public GameObject setupPanel;
    public Dialogue dialogueSystem;
    public PlayerController playerController; 

    [Header("UI 시각 효과 (색상)")]
    public TextMeshProUGUI[] optionTexts; // 0: 키보드/마우스, 1: 게임패드
    public Color normalColor = Color.gray;
    public Color selectedColor = Color.white;

    [Header("UI 시각 효과 (크기 및 속도)")]
    public float normalScale = 1.0f;
    public float selectedScale = 1.3f;
    public float animationSpeed = 10f;

    private int selectedIndex = 0;
    private bool stickMoved = false; 

    void Start()
    {
        // 1. 요청하신 대로 마우스를 시작부터 완전히 숨기고 화면 중앙에 가둡니다.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 플레이어 조작 잠금
        if (playerController != null) 
            playerController.enabled = false;
    }

    void Update()
    {
        HandleNavigation(); 
        HandleSelection();  
        AnimateHighlight(); 
    }

    private void HandleNavigation()
    {
        float vertical = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) vertical = 1f;
            if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame) vertical = -1f;
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.up.wasPressedThisFrame) vertical = 1f;
            if (Gamepad.current.dpad.down.wasPressedThisFrame) vertical = -1f;

            float stickY = Gamepad.current.leftStick.y.ReadValue();
            if (Mathf.Abs(stickY) > 0.5f && !stickMoved)
            {
                vertical = Mathf.Sign(stickY);
                stickMoved = true;
            }
            else if (Mathf.Abs(stickY) < 0.2f)
            {
                stickMoved = false;
            }
        }

        if (vertical > 0)
        {
            selectedIndex--;
            if (selectedIndex < 0) selectedIndex = optionTexts.Length - 1;
        }
        else if (vertical < 0)
        {
            selectedIndex++;
            if (selectedIndex >= optionTexts.Length) selectedIndex = 0;
        }
    }

    private void HandleSelection()
    {
        bool isConfirmPressed = false;

        if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
            isConfirmPressed = true;

        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) 
            isConfirmPressed = true;

        if (isConfirmPressed)
        {
            if (selectedIndex == 0) SelectKeyboardMouse();
            else if (selectedIndex == 1) SelectGamepad();
        }
    }

    private void AnimateHighlight()
    {
        for (int i = 0; i < optionTexts.Length; i++)
        {
            Color targetColor = (i == selectedIndex) ? selectedColor : normalColor;
            float targetScale = (i == selectedIndex) ? selectedScale : normalScale;
            Vector3 targetVectorScale = new Vector3(targetScale, targetScale, 1f);

            optionTexts[i].color = Color.Lerp(optionTexts[i].color, targetColor, Time.deltaTime * animationSpeed);
            optionTexts[i].transform.localScale = Vector3.Lerp(optionTexts[i].transform.localScale, targetVectorScale, Time.deltaTime * animationSpeed);
        }
    }

    // ── Null 에러 방지 및 직접 전달 로직 ──
    public void SelectKeyboardMouse()
    {
        ApplySelection(false);
    }

    public void SelectGamepad()
    {
        ApplySelection(true);
    }

    private void ApplySelection(bool isGamepad)
    {
        // GameManager가 존재하는지 먼저 확인합니다. (튜토리얼 씬만 단독 실행했을 때의 에러 방지)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentInputDevice = isGamepad 
                ? GameManager.InputDeviceType.Gamepad 
                : GameManager.InputDeviceType.KeyboardMouse;
        }

        // Dialogue 스크립트에 어떤 기기가 선택되었는지 "직접" 알려주고 시작합니다.
        if (dialogueSystem != null)
        {
            dialogueSystem.isGamepadMode = isGamepad; 
            dialogueSystem.StartDialogue();
        }

        // 설정 창 닫기 및 플레이어 조작 복원
        setupPanel.SetActive(false);

        if (playerController != null) 
            playerController.enabled = true;
    }
}