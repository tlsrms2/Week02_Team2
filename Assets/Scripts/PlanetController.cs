using UnityEngine;

public class PlanetController : MonoBehaviour
{
    [Header("자전 설정")]
    // 외부 스크립트에서 변수에 무분별하게 접근하지 못하게 막고, Inspector 창에서만 속도와 방향을 조절할 수 있도록 캡슐화합니다.
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private Vector3 rotationAxis = Vector3.up; // 기본값: Y축을 기준으로 회전

    void Update()
    {
        RotatePlanet();
    }

    // 회전 로직을 별도의 메서드로 분리하여 내부 구조를 명확하게 유지합니다.
    private void RotatePlanet()
    {
        // 행성 자신의 축을 기준으로 초당 rotationSpeed 만큼 부드럽게 회전시킵니다.
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }
}