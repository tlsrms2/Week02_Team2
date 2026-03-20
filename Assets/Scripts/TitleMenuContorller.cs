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

    public int _selectedIndex = 0;
    private bool _stickMoved = false;
    private bool _keyMoved = false;

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
        float vertical = Input.GetAxisRaw("Vertical");

        if (!_stickMoved && Mathf.Abs(vertical) > 0.5f)
        {
            _stickMoved = true;

            if (menuButtons == null || menuButtons.Length == 0) return;

            if (vertical > 0) SetSelected((_selectedIndex - 1 + menuButtons.Length) % menuButtons.Length);
            else SetSelected((_selectedIndex + 1) % menuButtons.Length);
        }

        if (Mathf.Abs(vertical) < 0.2f)
            _stickMoved = false;

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.JoystickButton0) ||
            Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            if (_selectedIndex >= 0 && _selectedIndex < menuButtons.Length)
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