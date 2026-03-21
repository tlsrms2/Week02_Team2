using UnityEngine;
using System.Collections.Generic; // List를 사용하기 위해 추가

public class Barrel : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 3.0f;           // 굴러가는 속도
    public float fallSpeed = 5.0f;           // 떨어지는 속도
    public float ladderDropChance = 0.3f;    // 사다리를 만났을 때 떨어질 확률 
    public float ladderWaitTime = 0.5f;      // 사다리에서 대기하는 시간 (초)

    [Header("레이어 설정")]
    public LayerMask groundLayer;            // 철골 바닥 레이어
    public LayerMask wallLayer;              // 벽(막힌 곳) 레이어
    public LayerMask ladderLayer;            // 사다리 레이어
    public LayerMask destroyZoneLayer;
    public LayerMask holderLayer;

    [Header("스프라이트")]
    public Transform visualTransform;        // 회전시킬 술통 스프라이트/모델 

    public Vector3 moveDirection = Vector3.right;  // 로컬 기준 이동 방향 (스포너에서 제어할 수 있도록 public 변경)
    private bool isFalling = false;
    private bool isWaitingAtLadder = false;        // 사다리 위에서 대기 중인지 여부
    private float ladderWaitTimer = 0f;            // 사다리 대기 타이머
    private bool isLadderFalling = false;          // 사다리를 타고 수직 낙하 중인지 여부
    private bool checkedCurrentLadder = false;     // 같은 사다리에서 중복 검사 방지
    private int groundContactCount = 0;            // 타일 경계선 버그 방지용
    private float ignoreGroundY = -9999f;          // 사다리를 탈 때 윗층 바닥에 걸리지 않도록 통과시키기 위한 Y 좌표 기준
    private Vector3 currentNormal;                 // 현재 밟고 있는 바닥의 법선(기울기) 벡터
    private List<Collider> currentGrounds = new List<Collider>(); // 현재 밟고 있는 바닥들
    private HashSet<GroundDirection> appliedGroundDirections = new HashSet<GroundDirection>(); // 이미 방향을 강제받은 바닥을 기억

    [Header("Slide")]
    [SerializeField] LayerMask targerLayer;
    [SerializeField] float slideTime;

    void Start()
    {
        currentNormal = transform.up; // 초기화

        // 넘어짐 완벽 방지: 코드로 Rigidbody의 모든 회전축 고정
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        }
    }

    void Update()
    {
        // 사다리 낙하 타이머 체크 (이동은 멈추지 않고 계속 진행)
        if (isWaitingAtLadder)
        {
            ladderWaitTimer -= Time.deltaTime;
            if (ladderWaitTimer <= 0f)
            {
                StartLadderFall(); // 대기 시간이 끝나면 실제 낙하 시작
            }
        }

        if (isFalling)
        {
            // 사다리에서 떨어질 때는 수직 낙하, 절벽에서 떨어질 때는 진행 방향 유지(포물선 낙하)하여 모서리 비비적거림 방지
            float horizontalSpeed = isLadderFalling ? 0f : moveSpeed;
            Vector3 worldMoveDir = transform.TransformDirection(moveDirection);
            Vector3 fallVelocity = (-transform.up * fallSpeed) + (worldMoveDir * horizontalSpeed);
            transform.Translate(fallVelocity * Time.deltaTime, Space.World);
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
        // 땅에 있거나, 절벽에서 떨어지는 중일 때 회전 (사다리 수직 낙하 시에는 정지)
        if (visualTransform != null && (!isFalling || !isLadderFalling))
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
            Destroy(gameObject); // 오타 수정: q -> Destroy
            return;
        }

        // 2. Holder 레이어인지 확인
        if (((1 << collision.gameObject.layer) & holderLayer) != 0)
        {
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
            return;
        }

        // 3. 설정된 대상 레이어가 아닌 오브젝트(Default 등)와 충돌 시 물리 연산 완전히 무시 (비비적거림 방지)
        int targetLayers = groundLayer | wallLayer | destroyZoneLayer | holderLayer | ladderLayer;
        if (((1 << collision.gameObject.layer) & targetLayers) == 0)
        {
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
            return;
        }

        // 사다리를 타기 시작한 직후(현재 층의 구멍을 빠져나가는 중) 만나는 모든 바닥 콜라이더를 완전히 통과(무시)하게 만듦
        // 이렇게 하면 좁은 틈새에 끼거나 건너편 모서리를 밟고 다시 굴러가버리는 버그가 완벽히 해결됩니다.
        if (isLadderFalling && transform.position.y > ignoreGroundY && ((1 << collision.gameObject.layer) & groundLayer) != 0)
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
            // 현재 밟고 있는 바닥 리스트에 추가
            if (!currentGrounds.Contains(collision.collider))
                currentGrounds.Add(collision.collider);

            groundContactCount++;
            
            // 바닥의 윗면(위쪽을 향하는 면)에 닿았을 때만 착지로 인정하여 땅 끝 모서리에서 비비적거리는 버그 완벽 방지
            if (Vector3.Dot(collision.contacts[0].normal, transform.up) > 0.1f)
            {
                currentNormal = collision.contacts[0].normal; // 바닥 기울기 저장
                if (isFalling)
                {
                    isFalling = false;

                    // 1. 바닥에 강제 방향 스크립트가 있는지 확인
                    GroundDirection gd = collision.gameObject.GetComponent<GroundDirection>();
                    if (gd != null && !appliedGroundDirections.Contains(gd))
                    {
                        moveDirection = gd.forceDirection.normalized;
                        appliedGroundDirections.Add(gd); // 기억해두기
                    }
                    else
                    {
                        // 2. 강제 방향이 없다면 기존처럼 바닥의 기울기에 따라 내리막길로 방향을 결정
                        // 법선의 X값이 양수면 경사가 오른쪽으로 낮아지므로 오른쪽으로 이동
                        if (currentNormal.x > 0.01f)
                            moveDirection = Vector3.right;
                        // 법선의 X값이 음수면 경사가 왼쪽으로 낮아지므로 왼쪽으로 이동
                        else if (currentNormal.x < -0.01f)
                            moveDirection = Vector3.left;
                    }
                        
                    isLadderFalling = false; // 착지 시 수직 낙하 상태 해제
                }
                else
                {
                    // 이미 굴러가고 있는 도중 강제 방향이 설정된 새로운 바닥을 밟았을 때도 적용
                    GroundDirection gd = collision.gameObject.GetComponent<GroundDirection>();
                    if (gd != null && !appliedGroundDirections.Contains(gd))
                    {
                        moveDirection = gd.forceDirection.normalized;
                        appliedGroundDirections.Add(gd); // 기억해두기
                    }
                }
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // 이동 중 바닥 기울기가 변할 수 있으므로 지속적으로 갱신
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            if (Vector3.Dot(collision.contacts[0].normal, transform.up) > 0.1f)
            {
                currentNormal = collision.contacts[0].normal;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // 3. 바닥에서 벗어났을 때 (절벽) 추락 시작
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
        // 밟고 있는 바닥 리스트에서 제거
        currentGrounds.Remove(collision.collider);

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
        // 4. 사다리를 만났을 때 (확률적 추락)
        bool isLadder = ((1 << other.gameObject.layer) & ladderLayer) != 0;
        
        if (isLadder && !checkedCurrentLadder)
        {
            checkedCurrentLadder = true;
            float roll = Random.value;
            // 확률이 1일 때 Random.value가 1.0이 나오는 예외 상황까지 포함하도록 <= 로 변경
            if (roll <= ladderDropChance)
            {
                isWaitingAtLadder = true; // 대기 상태 돌입
                ladderWaitTimer = ladderWaitTime; // 인스펙터에서 설정한 시간만큼 타이머 시작
            }
        }

        if((targerLayer.value &(1 << other.gameObject.layer)) != 0)
        {
            other.gameObject.GetComponent<PlayerHealth>().TakeSlide(slideTime);
        }
    }

    private void StartLadderFall()
    {
        isWaitingAtLadder = false;
        isFalling = true; // 사다리 타고 내려가기
        isLadderFalling = true; // 수직 낙하 상태 돌입
        
        // 현재 위치보다 1.2유닛 아래로 내려갈 때까지 만나는 현재 층의 바닥 모서리들을 모두 무시하도록 좌표 기록
        ignoreGroundY = transform.position.y - 1.2f;

        Collider myCollider = GetComponent<Collider>();
        foreach (Collider ground in currentGrounds.ToArray())
        {
            if (ground != null)
            {
                Physics.IgnoreCollision(myCollider, ground, true);
            }
        }
        currentGrounds.Clear(); // 밟고 있던 바닥 데이터 초기화
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & ladderLayer) != 0)
        {
            checkedCurrentLadder = false; // 사다리 벗어나면 초기화
        }
    }
}