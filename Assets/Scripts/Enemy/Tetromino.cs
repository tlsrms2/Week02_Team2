using UnityEngine;

public class Tetromino : MonoBehaviour
{
    [Header("낙하 설정")]
    public float fallSpeed = 4.0f;       // 떨어지는 속도
    public LayerMask stopLayer;          // 블록이 닿으면 멈추게 할 레이어 (바닥, 혹은 이미 쌓인 다른 블록)

    private bool isFalling = true;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 떨어지는 동안에는 3D 물리 엔진의 중력과 회전을 무시하고 스크립트로 제어합니다.
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    void Update()
    {
        if (isFalling)
        {
            // 로컬 공간 기준으로 수직 낙하 (3D 벽면의 기울기 유지)
            transform.Translate(-Vector3.up * fallSpeed * Time.deltaTime, Space.Self);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 1. 플레이어를 덮쳤을 때의 처리 (태그 확인)
        if (collision.gameObject.CompareTag("Player"))
        {
            // TODO: 플레이어 데미지 처리 또는 추락 이벤트 호출
            Debug.Log("플레이어가 테트리스 블록에 깔렸습니다!");
        }

        // 2. 바닥이나 다른 테트리스 블록에 닿았을 때 낙하 정지
        if (((1 << collision.gameObject.layer) & stopLayer) != 0)
        {
            if (isFalling)
            {
                isFalling = false;
                // 바닥에 안착하면 플레이어가 밟을 수 있는 고정된 지형이 되도록 설정
                if (rb != null) rb.isKinematic = true; 
            }
        }
    }
}