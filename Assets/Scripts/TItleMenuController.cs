using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TitleMenuController : MonoBehaviour
{
    [Header("버튼 목록 (위에서 아래 순서)")]
    [SerializeField] private TitleButton[] menuButtons;

    [Header("색상")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;

    private int _selectedIndex = 0;
    private bool _stickMoved = false;

    void Start()
    {
        // EventSystem의 키보드 입력 비활성화 (Space/Enter 가로채기 방지)
        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = false;
    }
    void Update()
    {
        HandleKeyboard();
        HandleGamepad();
        HandleMouse();
    }

    private void HandleKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            SetSelected((_selectedIndex - 1 + menuButtons.Length) % menuButtons.Length);
        }
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            SetSelected((_selectedIndex + 1) % menuButtons.Length);
        }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            menuButtons[_selectedIndex].OnClick();
        }
    }
    private void HandleGamepad()
    {
        float vertical = Input.GetAxisRaw("Vertical");

        // 스틱/D패드 위아래
        if (!_stickMoved && Mathf.Abs(vertical) > 0.5f)
        {
            _stickMoved = true;

            if (vertical > 0)
                SetSelected((_selectedIndex - 1 + menuButtons.Length) % menuButtons.Length);
            else
                SetSelected((_selectedIndex + 1) % menuButtons.Length);
        }

        // 스틱이 중립으로 돌아왔을 때 해제
        if (Mathf.Abs(vertical) < 0.2f)
            _stickMoved = false;

        // A버튼 (조이스틱 버튼 0) 또는 Start 버튼 (버튼 7)
        if (Input.GetKeyDown(KeyCode.JoystickButton0) ||
            Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            menuButtons[_selectedIndex].OnClick();
        }
    }


    private void HandleMouse()
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i].IsMouseOver())
            {
                SetSelected(i);

                if (Input.GetMouseButtonDown(0))
                    menuButtons[i].OnClick();
            }
        }
    }

    private void SetSelected(int index)
    {
        // 이전 선택 해제
        for (int i = 0; i < menuButtons.Length; i++)
            menuButtons[i].SetHighlight(false, normalColor);

        // 새 선택 적용
        _selectedIndex = index;
        menuButtons[_selectedIndex].SetHighlight(true, selectedColor);
    }

    public void InitMenu()
    {
        for (int i = 0; i < menuButtons.Length; i++)
            menuButtons[i].SetHighlight(false, normalColor);

        SetSelected(0);
        gameObject.SetActive(true);
    }
    public void OutMenu()
    {
        for (int i = 0; i < menuButtons.Length; i++)
            menuButtons[i].SetHighlight(false, normalColor);

        SetSelected(0);
        gameObject.SetActive(false);
    }
}