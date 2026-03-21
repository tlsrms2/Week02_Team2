using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TitleMenuController : MonoBehaviour
{
    [Header("버튼 목록 (위에서 아래 순서)")]
    [SerializeField] private TitleButton[] menuButtons;

    [Header("클릭 이벤트")]
    [SerializeField] public UnityEvent onClickEvent;


    [Header("색상")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip startSound;
    
    public int _selectedIndex = 0;
    private bool _stickMoved = false;
    private bool _isClickPlaying = false;
    bool anyHovered = false;

    private enum InputMode { Keyboard, Mouse }
    private InputMode _inputMode = InputMode.Keyboard;

    void Start()
    {
        // EventSystem의 키보드 입력 비활성화 (Space/Enter 가로채기 방지)
        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = false;
    }
    void Update()
    {
        HandleNavigation();
        HandleMouse();
    }


    private void HandleNavigation()
    {
        if (_isClickPlaying) return; // 입력 차단

        float vertical = Input.GetAxisRaw("Vertical");

        if (!_stickMoved && Mathf.Abs(vertical) > 0.5f)
        {
            _stickMoved = true;
            _inputMode = InputMode.Keyboard; // 키보드 모드로 전환

            if (menuButtons == null || menuButtons.Length == 0) return;

            if (vertical > 0) SetSelected((_selectedIndex - 1 + menuButtons.Length) % menuButtons.Length);
            else SetSelected((_selectedIndex + 1) % menuButtons.Length);
            
            // 소리 실행
            PlayMoveSound();
        }

        if (Mathf.Abs(vertical) < 0.2f)
            _stickMoved = false;

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.JoystickButton0) ||
            Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            if (_selectedIndex >= 0 && _selectedIndex < menuButtons.Length)
                StartCoroutine(PlaySoundThenClick(menuButtons[_selectedIndex]));
        }
    }


    private void HandleMouse()
    {
        if (_isClickPlaying) return; // 입력 차단

        // 마우스가 움직였을 때만 마우스 모드로 전환
        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
            _inputMode = InputMode.Mouse;

        if (_inputMode != InputMode.Mouse) return; // 키보드 모드면 마우스 무시
        anyHovered = false;

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i].IsMouseOver())
            {
                anyHovered = true;

                // 다른 버튼으로 이동할 때 색상 + 화살표 변경 + 사운드
                if (i != _selectedIndex)
                {
                    SetSelected(i); // 색상 + 화살표 자동 처리
                    PlayMoveSound();
                }

                if (Input.GetMouseButtonDown(0))
                    StartCoroutine(PlaySoundThenClick(menuButtons[i]));
            }
        }

    }
    // 게임 시작 소리
    private IEnumerator PlaySoundThenClick(TitleButton button)
    {
        _isClickPlaying = true;

        

        // 사운드 재생
        if (audioSource != null && startSound != null)
        {
            // 버튼 하이라이트
            button.PlaySelectEffect();

            audioSource.PlayOneShot(startSound);
            // 사운드 길이만큼 대기
            yield return new WaitForSeconds(startSound.length);
        }

        // 사운드 끝난 후 OnClick 실행
        button.OnClick();
        _isClickPlaying = false;
    }
    // 움직임 소리
    private void PlayMoveSound()
    {
        if (audioSource != null && moveSound != null)
            audioSource.PlayOneShot(moveSound);
    }

    private void SetSelected(int index)
    {
        // 범위 강제 클램프
        index = Mathf.Clamp(index, 0, menuButtons.Length - 1);

        // 이전 선택 해제
        for (int i = 0; i < menuButtons.Length; i++)
            menuButtons[i].SetHighlight(false, normalColor);

        // 새 선택 적용
        _selectedIndex = index;
        menuButtons[_selectedIndex].SetHighlight(true, selectedColor);
    }

    public void InitMenu()
    {
        gameObject.SetActive(true);
        for (int i = 0; i < menuButtons.Length; i++)
            menuButtons[i].SetHighlight(false, normalColor);

        SetSelected(0);
    }
    public void OutMenu()
    {
        for (int i = 0; i < menuButtons.Length; i++)
            menuButtons[i].SetHighlight(false, normalColor);

        SetSelected(0);
        gameObject.SetActive(false);
    }
}