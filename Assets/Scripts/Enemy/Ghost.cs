using UnityEngine;

public class Ghost : MonoBehaviour
{
    public float speed = 2.0f;       // 유령 이동 속도
    public float rayDistance = 1.0f; // 레이저 길이 (타일 크기에 맞게 조절)
    public LayerMask wallLayer;      // 인스펙터에서 MazeWall 레이어 선택

    [Header("Sprites")]
    public Sprite upSprite;          // 위쪽을 볼 때의 스프라이트
    public Sprite downSprite;        // 아래쪽을 볼 때의 스프라이트
    public Sprite leftSprite;        // 왼쪽을 볼 때의 스프라이트
    public Sprite rightSprite;       // 오른쪽을 볼 때의 스프라이트

    private SpriteRenderer spriteRenderer;
    private Vector3 moveDirection = Vector3.up; // 현재 이동 방향 (로컬 기준)

    void Start()
    {
        // 유령 오브젝트가 가지고 있는 SpriteRenderer 컴포넌트를 가져옵니다.
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // 특정 방향에 벽이 있는지 확인하는 함수
    bool CheckWall(Vector3 direction)
    {
        // 유령의 현재 위치에서 direction 방향으로 레이저를 쏩니다.
        // 방향 전환 시 transform을 회전하지 않으므로, 로컬 방향(direction)을 월드 방향으로 변환하여 사용합니다.
        Vector3 worldDirection = transform.TransformDirection(direction);
        if (Physics.Raycast(transform.position, worldDirection, out RaycastHit hit, rayDistance, wallLayer))
        {
            return true; // 벽에 막힘
        }
        return false; // 길이 뚫려 있음
    }

    void Update()
    {
        // 앞이 막혀있는지 확인
        if (CheckWall(moveDirection))
        {
            // 현재 방향을 기준으로 오른쪽, 왼쪽 방향 벡터 계산
            Vector3 rightDir = new Vector3(moveDirection.y, -moveDirection.x, 0);
            Vector3 leftDir = new Vector3(-moveDirection.y, moveDirection.x, 0);

            // 오른쪽, 왼쪽 방향에 벽이 있는지 확인
            bool canGoRight = !CheckWall(rightDir);
            bool canGoLeft = !CheckWall(leftDir);

            if (canGoRight && canGoLeft)
            {
                // 양쪽 다 뚫려있다면 랜덤하게 좌/우 중 하나 선택
                moveDirection = Random.Range(0, 2) == 0 ? leftDir : rightDir;
            }
            else if (canGoRight)
            {
                // 오른쪽만 뚫려있다면 오른쪽으로 방향 전환
                moveDirection = rightDir;
            }
            else if (canGoLeft)
            {
                // 왼쪽만 뚫려있다면 왼쪽으로 방향 전환
                moveDirection = leftDir;
            }
            else
            {
                // 앞, 좌, 우 모두 막힌 막다른 길이면 180도 뒤로 돌기
                moveDirection = -moveDirection;
            }
        }
        
        UpdateSprite(); // 이동 방향에 맞게 스프라이트 갱신

        // 선택된 방향으로 계속 직진
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.Self); 
    }

    // 현재 이동 방향을 확인하여 스프라이트 이미지를 교체하는 함수
    void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        // 정밀도 문제 방지를 위해 0.5를 기준으로 방향을 확인합니다.
        if (moveDirection.y > 0.5f)
            spriteRenderer.sprite = upSprite;
        else if (moveDirection.y < -0.5f)
            spriteRenderer.sprite = downSprite;
        else if (moveDirection.x > 0.5f)
            spriteRenderer.sprite = rightSprite;
        else if (moveDirection.x < -0.5f)
            spriteRenderer.sprite = leftSprite;
    }
}
