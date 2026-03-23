using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

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

    [Header("선택 후 추가 대사 설정")]
    public GameObject SetupQuestion;

    public GameObject RecommendText;
    public TextMeshProUGUI additionalDialogueText; // 추가 대사를 띄울 UI 텍스트 연결
    public Dialogue.DialogueEntry[] additionalDialogues; // 인스펙터에서 대사 목록 작성
    public float typeSpeed = 0.05f;
    [Range(0f, 0.5f)] public float pitchRange = 0.1f;
    public AudioSource typeAudioSource;
    public AudioClip typeSound;

    private int selectedIndex = 0;
    private bool stickMoved = false; 
    private bool isDialoguePlaying = false;

    void Start()
    {
        // 1. 요청하신 대로 마우스를 시작부터 완전히 숨기고 화면 중앙에 가둡니다.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 플레이어 조작 잠금
        if (playerController != null) 
            playerController.enabled = false;
            
        if (additionalDialogueText != null)
            additionalDialogueText.text = "";
    }

    void Update()
    {
        if (isDialoguePlaying) return; // 대사가 재생 중일 때는 입력 차단

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
        }

        // 바로 넘기지 않고 추가 대사 연출 시작
        StartCoroutine(PlayAdditionalDialogueRoutine(isGamepad));
    }

    private IEnumerator PlayAdditionalDialogueRoutine(bool isGamepad)
    {
        isDialoguePlaying = true;

        // 기존 선택지 텍스트 숨기기

        SetupQuestion.SetActive(false);
        RecommendText.SetActive(false);
        foreach (var text in optionTexts)
        {
            text.gameObject.SetActive(false);
        }

        if (additionalDialogues != null && additionalDialogues.Length > 0 && additionalDialogueText != null)
        {
            for (int i = 0; i < additionalDialogues.Length; i++)
            {
                string processedSentence = ProcessTags(additionalDialogues[i].sentence, isGamepad);
                yield return StartCoroutine(TypeText(processedSentence));
                yield return new WaitForSeconds(additionalDialogues[i].duration);
                additionalDialogueText.text = "";
            }
        }

        FinishSetup();
    }

    private string ProcessTags(string originalText, bool isGamepad)
    {
        // Dialogue와 동일하게 추가 대사에서도 {LH}, {RH} 등의 키보드/패드 태그 치환을 지원합니다.
        string lHand = isGamepad ? "[LB]" : "[Q]";
        string rHand = isGamepad ? "[RB]" : "[E]";
        string lFoot = isGamepad ? "[LT]" : "[A]";
        string rFoot = isGamepad ? "[RT]" : "[D]";

        string result = originalText;
        result = result.Replace("{LH}", $"<color=#50FF50><b>{lHand}</b></color>");
        result = result.Replace("{RH}", $"<color=#50FF50><b>{rHand}</b></color>");
        result = result.Replace("{LF}", $"<color=#50FF50><b>{lFoot}</b></color>");
        result = result.Replace("{RF}", $"<color=#50FF50><b>{rFoot}</b></color>");

        return result;
    }

    private IEnumerator TypeText(string targetText)
    {
        int charIndex = 0;
        additionalDialogueText.text = "";

        while (charIndex < targetText.Length)
        {
            int randomStep = Random.Range(1, 4);
            int charsAddedThisStep = 0;

            while (charsAddedThisStep < randomStep && charIndex < targetText.Length)
            {
                if (targetText[charIndex] == '<')
                {
                    int tagEndIndex = targetText.IndexOf('>', charIndex);
                    if (tagEndIndex != -1)
                    {
                        string fullTag = targetText.Substring(charIndex, tagEndIndex - charIndex + 1);
                        additionalDialogueText.text += fullTag;
                        charIndex = tagEndIndex + 1;
                        continue; 
                    }
                }

                additionalDialogueText.text += targetText[charIndex];
                charIndex++;
                charsAddedThisStep++;
            }

            if (charsAddedThisStep > 0 && typeAudioSource != null && typeSound != null)
            {
                typeAudioSource.pitch = Random.Range(1f - pitchRange, 1f + pitchRange);
                typeAudioSource.PlayOneShot(typeSound);
            }

            yield return new WaitForSeconds(typeSpeed);
        }
    }

    private void FinishSetup()
    {
        SoundManager.Instance.PlayTutorialBgm();

        // 모든 추가 대사가 끝난 후 본 게임 대사 시작 및 UI 정리
        if (dialogueSystem != null)
        {
            dialogueSystem.StartDialogue();
        }

        setupPanel.SetActive(false);

        if (playerController != null) 
            playerController.enabled = true;
            
        isDialoguePlaying = false;
    }
}