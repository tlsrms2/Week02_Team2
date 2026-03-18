using UnityEngine;
using UnityEngine.InputSystem; // 새로운 인풋 시스템을 사용하기 위한 네임스페이스입니다.

public class SpacePlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 20f; 
    [SerializeField] private float fastMoveMultiplier = 3f; 

    [Header("회전 설정")]
    // 신형 인풋 시스템에 맞춰 기본 감도를 낮게 설정했습니다.
    [SerializeField] private float mouseSensitivity = 0.2f; 

    private float rotationX = 0f;
    private float rotationY = 0f;

    // 외부에서 입력을 함부로 조작하지 못하도록 입력 액션들을 private으로 캡슐화합니다.
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction upAction;
    private InputAction downAction;
    private InputAction fastAction;

    void Awake()
    {
        // 외부 에셋에 의존하지 않고, 코드 내부에서 입력 키와 액션을 매핑하여 독립성을 높입니다.
        moveAction = new InputAction("Move");
        moveAction.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        lookAction = new InputAction("Look", binding: "<Mouse>/delta");
        
        upAction = new InputAction("Up", binding: "<Keyboard>/e");
        downAction = new InputAction("Down", binding: "<Keyboard>/q");
        
        fastAction = new InputAction("Fast", binding: "<Keyboard>/leftShift");
    }

    // 신형 인풋 시스템은 사용 전후로 반드시 액션을 활성화(Enable) 및 비활성화(Disable) 해야 합니다.
    void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        upAction.Enable();
        downAction.Enable();
        fastAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        upAction.Disable();
        downAction.Disable();
        fastAction.Disable();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleRotation();
        HandleMovement();
    }

    private void HandleRotation()
    {
        // 마우스의 X, Y 이동 변화량(Delta)을 2D 벡터로 읽어옵니다.
        Vector2 lookDelta = lookAction.ReadValue<Vector2>() * mouseSensitivity;

        rotationY += lookDelta.x;
        rotationX -= lookDelta.y;

        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }

    private void HandleMovement()
    {
        // WASD 키의 입력을 2D 벡터(X, Y)로 읽어옵니다.
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        float moveY = 0f;

        // IsPressed()를 통해 키가 눌려있는지 직관적으로 확인합니다.
        if (upAction.IsPressed()) moveY = 1f;
        if (downAction.IsPressed()) moveY = -1f;

        // moveInput.x는 좌우(X축), moveInput.y는 전후(Z축)에 해당합니다.
        Vector3 moveDirection = new Vector3(moveInput.x, moveY, moveInput.y);
        moveDirection = transform.TransformDirection(moveDirection);

        float currentSpeed = moveSpeed;
        if (fastAction.IsPressed())
        {
            currentSpeed *= fastMoveMultiplier;
        }

        transform.position += moveDirection * currentSpeed * Time.deltaTime;
    }
}