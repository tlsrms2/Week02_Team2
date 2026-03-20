using UnityEngine;

public class Barrel : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 3.0f;           // 굴러가는 속도
    public float fallSpeed = 5.0f;           // 떨어지는 속도
    public float ladderDropChance = 0.3f;    // 사다리를 만났을 때 떨어질 확률 

    [Header("레이어 설정")]
    public LayerMask groundLayer;            // 철골 바닥 레이어
    public LayerMask wallLayer;              // 벽(막힌 곳) 레이어
    public LayerMask ladderLayer;            // 사다리 레이어
    public LayerMask destroyZoneLayer;
    public LayerMask holderLayer;

    [Header("스프라이트")]
    public Transform visualTransform;        // 회전시킬 술통 스프라이트/모델 

    private Vector3 moveDirection = Vector3.right; // 로컬 기준 이동 방향
    private bool isFalling = false;
    private bool checkedCurrentLadder = false;     // 같은 사다리에서 중복 검사 방지
    private int groundContactCount = 0;            // 타일 경계선 버그 방지용
    private Vector3 currentNormal;                 // 현재 밟고 있는 바닥의 법선(기울기) 벡터

    void Start()
    {
        currentNormal = transform.up; // 초기화

        // 넘어짐 완벽 방지: 코드로 Rigidbody의 모든 회전축 고정
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    void Update()
    {
        if (isFalling)
        {
            // 아래로 수직 낙하
            transform.Translate(-Vector3.up * fallSpeed * Time.deltaTime, Space.Self);
        }
        else
        {
            // 경사면을 따라 부드럽게 이동하도록 벡터 투영 (ProjectOnPlane)
            Vector3 worldMoveDir = transform.TransformDirection(moveDirection);
            Vector3 slopeMoveDir = Vector3.ProjectOnPlane(worldMoveDir, currentNormal).normalized;
            
            // 계산된 경사면 방향으로 이동 (Space.World 사용)
            transform.Translate(slopeMoveDir * moveSpeed * Time.deltaTime, Space.World);
        }

        // 시각적으로 굴러가는 효과 (Z축 회전)
        if (visualTransform != null && !isFalling)
        {
            float rotDirection = moveDirection.x > 0 ? -1f : 1f;
            visualTransform.Rotate(0, 0, rotDirection * moveSpeed * 100f * Time.deltaTime, Space.Self);
        }
    }

    // --- 콜라이더 충돌 이벤트 ---

    private void OnCollisionEnter(Collision collision)
    {
        // 1. DestroyZone 레이어인지 확인 (비트 연산 사용)
        if (((1 << collision.gameObject.layer) & destroyZoneLayer) != 0)
        {
            Destroy(gameObject);
            return;
        }

        // 2. Holder 레이어인지 확인
        if (((1 << collision.gameObject.layer) & holderLayer) != 0)
        {
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
            return;
        }

        // 1. 벽에 닿았을 때 방향 반전
        if (((1 << collision.gameObject.layer) & wallLayer) != 0)
        {
            // 바닥(경사면)을 벽으로 잘못 인식해 왔다갔다 하는 버그 방지 (측면 충돌일 때만 인정)
            if (Mathf.Abs(Vector3.Dot(collision.contacts[0].normal, transform.up)) < 0.5f)
            {
                moveDirection = -moveDirection;
            }
        }

        // 2. 바닥에 닿았을 때 추락 멈춤
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            groundContactCount++;
            currentNormal = collision.contacts[0].normal; // 바닥 기울기 저장
            if (isFalling)
            {
                isFalling = false;
                moveDirection = Random.value > 0.5f ? Vector3.right : Vector3.left; // 착지 시 랜덤 방향
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // 이동 중 바닥 기울기가 변할 수 있으므로 지속적으로 갱신
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            currentNormal = collision.contacts[0].normal;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // 3. 바닥에서 벗어났을 때 (절벽) 추락 시작
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            groundContactCount--;
            // 안전 장치: 여러 개의 바닥 타일 경계를 넘어갈 때 곧바로 추락하는 버그 방지
            if (groundContactCount <= 0)
            {
                groundContactCount = 0;
                isFalling = true;
                currentNormal = transform.up; // 공중에 있으므로 기본 방향으로 초기화
            }
        }
    }

    // 사다리는 통과해야 하므로 트리거(Is Trigger)로 설정되어 있다고 가정합니다.
    private void OnTriggerEnter(Collider other)
    {
        // destroyzone이 트리거(Is Trigger)로 설정된 경우를 대비한 파괴 처리
        if (other.CompareTag("destroyzone"))
        {
            Destroy(gameObject);
            return;
        }

        // 4. 사다리를 만났을 때 (확률적 추락)
        if (((1 << other.gameObject.layer) & ladderLayer) != 0 && !checkedCurrentLadder)
        {
            checkedCurrentLadder = true;
            if (Random.value < ladderDropChance)
            {
                isFalling = true; // 사다리 타고 내려가기
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & ladderLayer) != 0)
        {
            checkedCurrentLadder = false; // 사다리 벗어나면 초기화
        }
    }
}