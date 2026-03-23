using UnityEngine;
using UnityEngine.UI;

public class SpeedController : MonoBehaviour
{
    public static SpeedController Instance;

    [Header("슬라이더")]
    [SerializeField] private Slider sensitivitySlider;

    [Header("감도 설정")]
    [SerializeField] private float minMultiplier = 0.5f;  // 슬라이더 0일 때 배율
    [SerializeField] private float maxMultiplier = 3f;    // 슬라이더 1일 때 배율

    // 외부에서 배율 읽기
    public float SensitivityMultiplier { get; private set; } = 1f;

    private bool _sliderActive = false;
    private bool _stickMoved = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 기존 Instance의 슬라이더를 새 씬의 슬라이더로 교체
            Instance.UpdateSlider(sensitivitySlider);
            Destroy(gameObject);
            return;
        }
    }

    // 씬 전환 후 새 슬라이더 연결
    public void UpdateSlider(Slider newSlider)
    {
        if (newSlider == null) return;

        // 기존 슬라이더 리스너 제거
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.RemoveListener(OnSliderChanged);


        // 저장된 값으로 노브 위치 복원
        float saved = PlayerPrefs.GetFloat("SensitivitySlider", 0.5f);
        sensitivitySlider.value = saved;
        sensitivitySlider.onValueChanged.AddListener(OnSliderChanged);
    }

    void Start()
    {
        if (Instance != this) return; // 중복 오브젝트면 Start 스킵

        float saved = PlayerPrefs.GetFloat("SensitivitySlider", 0.5f);

        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = saved;
            sensitivitySlider.onValueChanged.AddListener(OnSliderChanged);
        }

        ApplyMultiplier(saved);
    }

    void Update()
    {
        HandleKeyboard();
        HandleGamepad();
    }

    private void HandleKeyboard()
    {
        // 슬라이더 활성화 상태일 때만 조작 허용
        if (!_sliderActive) return;

        if (sensitivitySlider == null) return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            AdjustSlider(0.05f);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            AdjustSlider(-0.05f);
    }

    private void HandleGamepad()
    {
        if (sensitivitySlider == null) return;

        float vertical = Input.GetAxisRaw("Vertical");

        if (!_stickMoved && Mathf.Abs(vertical) > 0.5f)
        {
            _stickMoved = true;
            AdjustSlider(vertical > 0 ? 0.05f : -0.05f);
        }

        if (Mathf.Abs(vertical) < 0.2f)
            _stickMoved = false;
    }

    private void AdjustSlider(float delta)
    {
        if (sensitivitySlider == null) return;

        sensitivitySlider.value = Mathf.Clamp01(sensitivitySlider.value + delta);
    }

    private void OnSliderChanged(float value)
    {
        ApplyMultiplier(value);
        PlayerPrefs.SetFloat("SensitivitySlider", value);
    }

    private void ApplyMultiplier(float sliderValue)
    {
        // 0~1 값을 minMultiplier~maxMultiplier 범위로 변환
        SensitivityMultiplier = Mathf.Lerp(minMultiplier, maxMultiplier, sliderValue);
    }

    public void EnableSliderControl()
    {
        _sliderActive = true;
    }

    public void DisableSliderControl()
    {
        _sliderActive = false;
    }
}