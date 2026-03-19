using UnityEngine;

public class DrumControlle : MonoBehaviour
{
    public Transform destination; // 인스펙터 창에서 할당할 목적지
    public float moveSpeed = 5f;  // 이동 속도

    void Update()
    {
        // 목적지가 할당되어 있다면
        if (destination != null)
        {
            // 현재 위치에서 목적지를 향해 직선으로 일정한 속도로 이동
            transform.position = Vector3.MoveTowards(transform.position, destination.position, moveSpeed * Time.deltaTime);
            
            // 목적지에 완전히 도착했을 때 오브젝트를 없애고 싶다면 아래 주석 해제
            /*
            if (Vector3.Distance(transform.position, destination.position) < 0.1f)
            {
                Destroy(gameObject);
            }
            */
        }
    }

    // Is Trigger가 체크된 콜라이더와 부딪혔을 때 감지
    void OnTriggerEnter(Collider other)
    {
        // 부딪힌 대상의 태그가 Player인지 확인
        if (other.CompareTag("Player"))
        {
            Debug.Log("플레이어와 충돌! 데미지 처리");
            
            // 충돌 후 드럼통을 파괴하려면 아래 코드 사용
            // Destroy(gameObject);
        }
    }
}