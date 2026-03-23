using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleButton : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Outline outline;          // Button Image에 붙은 Outline 컴포넌트
    [SerializeField] private TextMeshProUGUI arrow;    // "▶" 화살표 TMP

    [Header("클릭 이벤트")]
    [SerializeField] public UnityEvent onClickEvent;

    [Header("선택 효과")]
    [SerializeField] private int blinkCount = 3;         // 점멸 횟수
    [SerializeField] private float blinkInterval = 0.08f;// 점멸 간격


    private RectTransform _rect;
    private Vector3 _originalScale;
    private Coroutine _effectCoroutine;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    public void SetHighlight(bool on, Color color)
    {
        // 텍스트 색상
        label.color = color;

        // 아웃라인
        if (outline != null)
            outline.enabled = on;

        // 화살표
        if (arrow != null)
            arrow.gameObject.SetActive(on);

        // 효과 중단 후 원래 크기 복구
        if (_effectCoroutine != null)
        {
            StopCoroutine(_effectCoroutine);
            _effectCoroutine = null;
            _rect.localScale = _originalScale;
        }
    }
    // 선택 확정 시 외부에서 호출
    public void PlaySelectEffect()
    {
        if (_effectCoroutine != null)
        {
            StopCoroutine(_effectCoroutine);
            _rect.localScale = _originalScale;
        }
        _effectCoroutine = StartCoroutine(SelectEffect());
    }
    private IEnumerator SelectEffect()
    {
        for (int i = 0; i < blinkCount; i++)
        {
            label.enabled = false;
            yield return new WaitForSeconds(blinkInterval);

            label.enabled = true;
            yield return new WaitForSeconds(blinkInterval);
        }
        label.enabled = true;
    }

    public bool IsMouseOver()
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
        _rect,
        Input.mousePosition,
        null // Screen Space Overlay → null
    );
    }

    public void OnClick()
    {
        // 효과 코루틴 강제 종료
        if (_effectCoroutine != null)
        {
            StopCoroutine(_effectCoroutine);
            _effectCoroutine = null;
        }

        // label 강제 활성화
        label.enabled = true;

        onClickEvent?.Invoke();
    }

    // 인게임 씬 리스타트 버튼
    public void ReStart_Button(string name)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void mainMenu_Button()
    {
        Time.timeScale = 1f;
        ResetCutScenePrefs(3);
        SceneManager.LoadScene("TitleScene");

    }
    public void inGameMenu_Button()
    {
        Time.timeScale = 1f;
        ResetCutScenePrefs(3);
        SceneManager.LoadScene("inGameMainMenu");

    }
    // 모든 컷신 기록 초기화
    public void ResetCutScenePrefs(int maxStage)
    {
        for (int i = 1; i <= maxStage; i++)
        {
            PlayerPrefs.DeleteKey("CutSceneSeen_" + i);
        }
        PlayerPrefs.Save();
    }

    // 전체 PlayerPrefs 초기화
    public void ResetAllPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}