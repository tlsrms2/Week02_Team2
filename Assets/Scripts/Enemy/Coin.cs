using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("이펙트 설정")]
    public GameObject coinBurstEffect;
    private void OnTriggerEnter(Collider other)
    {
        // 충돌한 객체가 플레이어인지 확인 (대소문자 일치 필요)
        if (other.CompareTag("Player"))
        {
            // 플레이어가 코인을 먹었으므로 코인 오브젝트 파괴
            CollectCoin();
        }
    }

    private void CollectCoin()
    {
        if (coinBurstEffect != null)
        {
            Instantiate(coinBurstEffect, transform.position, transform.rotation);
        }

        Destroy(gameObject);
    }
}
