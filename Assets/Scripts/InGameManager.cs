using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class InGameManager : MonoBehaviour
{
    [Header("퍼즈 목록 (위에서 아래 순서)")]
    [SerializeField] private TitleButton[] pauseButtons;

    [Header("게임오버 목록 (위에서 아래 순서)")]
    [SerializeField] private TitleButton[] gameOverButtons;

    [Header("색상")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;

    [Header("사운드")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip startSound;

    [Header("퍼즈")]
    [SerializeField] private GameObject pausePanel;

    [Header("플레이어")]
    [SerializeField] private PlayerController playerController;

    private int _pauseSelectedIndex = 0;
    private int _gameOverSelectedIndex = 0;
    private bool _stickMoved = false;
    private bool _isClickPlaying = false;
    private bool _isPaused = false;
    private bool _isGameOver = false;

    private enum InputMode { Keyboard, Mouse }
    private InputMode _inputMode = InputMode.Keyboard;

    void Start()
    {
        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    void Update()
    {
        if (_isGameOver)
        {
            // 게임오버 상태 → 게임오버 버튼 조작
            HandleNavigation(gameOverButtons, ref _gameOverSelectedIndex);
            HandleMouse(gameOverButtons, ref _gameOverSelectedIndex);
            return;
        }

        HandlePause();

        if (_isPaused)
        {
            // 퍼즈 상태 → 퍼즈 버튼 조작
            HandleNavigation(pauseButtons, ref _pauseSelectedIndex);
            HandleMouse(pauseButtons, ref _pauseSelectedIndex);
        }
    }

    // ── 퍼즈 처리 ──────────────────────────────────

    private void HandlePause()
    {
        if (_isGameOver) return;

        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            if (_isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void SetGameOver()
    {
        _isGameOver = true;

        if (playerController != null)
            playerController.SetInputBlocked(true);

        if (_isPaused)
        {
            _isPaused = false;
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        // 게임오버 버튼 초기화
        InitButtons(gameOverButtons, ref _gameOverSelectedIndex);
    }

    private void PauseGame()
    {
        _isPaused = true;
        Time.timeScale = 0f;

        if (playerController != null)
            playerController.SetInputBlocked(true);

        if (pausePanel != null)
            pausePanel.SetActive(true);

        // 퍼즈 버튼 초기화
        InitButtons(pauseButtons, ref _pauseSelectedIndex);
    }

    public void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        if (playerController != null)
            playerController.SetInputBlocked(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        for (int i = 0; i < pauseButtons.Length; i++)
            pauseButtons[i].SetHighlight(false, normalColor);
    }

    // ── 공통 네비게이션 ────────────────────────────

    private void HandleNavigation(TitleButton[] buttons, ref int selectedIndex)
    {
        if (_isClickPlaying) return;
        if (buttons == null || buttons.Length == 0) return;

        float vertical = Input.GetAxisRaw("Vertical");

        if (!_stickMoved && Mathf.Abs(vertical) > 0.5f)
        {
            _stickMoved = true;
            _inputMode = InputMode.Keyboard;

            if (vertical > 0)
                SetSelected(buttons, ref selectedIndex,
                    (selectedIndex - 1 + buttons.Length) % buttons.Length);
            else
                SetSelected(buttons, ref selectedIndex,
                    (selectedIndex + 1) % buttons.Length);

            PlayMoveSound();
        }

        if (Mathf.Abs(vertical) < 0.2f)
            _stickMoved = false;

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            if (selectedIndex >= 0 && selectedIndex < buttons.Length)
                StartCoroutine(PlaySoundThenClick(buttons[selectedIndex]));
        }
    }

    private void HandleMouse(TitleButton[] buttons, ref int selectedIndex)
    {
        if (_isClickPlaying) return;
        if (buttons == null || buttons.Length == 0) return;

        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
            _inputMode = InputMode.Mouse;

        if (_inputMode != InputMode.Mouse) return;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].IsMouseOver())
            {
                if (i != selectedIndex)
                {
                    SetSelected(buttons, ref selectedIndex, i);
                    PlayMoveSound();
                }

                if (Input.GetMouseButtonDown(0))
                    StartCoroutine(PlaySoundThenClick(buttons[i]));
            }
        }
    }

    // ── 공통 선택 처리 ─────────────────────────────

    private void SetSelected(TitleButton[] buttons, ref int selectedIndex, int index)
    {
        index = Mathf.Clamp(index, 0, buttons.Length - 1);

        for (int i = 0; i < buttons.Length; i++)
            buttons[i].SetHighlight(false, normalColor);

        selectedIndex = index;
        buttons[selectedIndex].SetHighlight(true, selectedColor);
    }

    private void InitButtons(TitleButton[] buttons, ref int selectedIndex)
    {
        selectedIndex = 0;

        for (int i = 0; i < buttons.Length; i++)
            buttons[i].SetHighlight(false, normalColor);

        SetSelected(buttons, ref selectedIndex, 0);
    }

    // ── 공통 ───────────────────────────────────────

    private IEnumerator PlaySoundThenClick(TitleButton button)
    {
        _isClickPlaying = true;

        if (audioSource != null && startSound != null)
        {
            button.PlaySelectEffect();
            audioSource.PlayOneShot(startSound);
            yield return new WaitForSecondsRealtime(startSound.length);
        }

        button.OnClick();
        _isClickPlaying = false;
    }

    private void PlayMoveSound()
    {
        if (audioSource != null && moveSound != null)
            audioSource.PlayOneShot(moveSound);
    }

    public void OutMenu()
    {
        if (pauseButtons != null)
            for (int i = 0; i < pauseButtons.Length; i++)
                pauseButtons[i].SetHighlight(false, normalColor);

        gameObject.SetActive(false);
    }
}