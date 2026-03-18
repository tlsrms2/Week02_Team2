using UnityEngine;

public class OrbitController : MonoBehaviour
{
    [Header("공전 대상 및 궤도 설정")]
    [SerializeField] private Transform targetStar; // 공전의 중심이 될 대상입니다.
    [SerializeField] private float orbitSpeed = 10f; // 공전 속도입니다.
    [SerializeField] private float orbitRadius = 50f; // 중심으로부터 떨어진 궤도 반지름입니다.
    [SerializeField] private float startingAngle = 0f; // 각 행성마다 겹치지 않게 출발 각도를 지정합니다.

    // 내부 연산을 위해 외부로 노출하지 않고 캡슐화한 현재 각도 변수입니다.
    private float currentAngle;

    void Start()
    {
        // 게임 시작 시 인스펙터에서 설정한 초기 각도를 내부 변수에 적용합니다.
        currentAngle = startingAngle;
    }

    void Update()
    {
        // 공전 로직을 Update 안에서 직접 길게 쓰지 않고 추상화된 메서드로 호출하여 전체 흐름을 깔끔하게 유지합니다.
        PerformOrbit();
    }

    private void PerformOrbit()
    {
        // 타겟이 할당되지 않아 발생하는 NullReferenceException 에러(2차 문제)를 사전에 차단합니다.
        if (targetStar == null) return;

        // 시간에 따라 각도를 증가시킵니다.
        currentAngle += orbitSpeed * Time.deltaTime;
        
        // 각도가 무한히 커져 오버플로우가 발생하는 것을 막기 위해 360도가 넘으면 0으로 초기화합니다.
        if (currentAngle >= 360f) currentAngle -= 360f;

        // 삼각함수(Sin, Cos)를 이용하여 중심점 기준의 X, Z 평면상 원형 좌표를 계산합니다.
        float radian = currentAngle * Mathf.Deg2Rad;
        float x = Mathf.Cos(radian) * orbitRadius;
        float z = Mathf.Sin(radian) * orbitRadius;

        // 중심 대상(태양)의 위치를 기준으로 계산된 궤도 위치를 매 프레임 행성에게 강제로 적용시킵니다.
        transform.position = targetStar.position + new Vector3(x, 0f, z);
    }

    // 에디터의 씬(Scene) 뷰에서만 실행되며, 궤도 반경을 미리 확인할 수 있게 선을 그려줍니다.
    private void OnDrawGizmos()
    {
        if (targetStar == null) return;

        // 기즈모 선의 색상을 눈에 띄는 청록색으로 설정합니다.
        Gizmos.color = Color.cyan;

        // 부드러운 원을 그리기 위해 360도를 36개의 선분으로 쪼갭니다.
        int segments = 36;
        float angleStep = 360f / segments;

        // 첫 번째 선분의 시작점을 임시로 저장할 변수입니다.
        Vector3 previousPoint = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float radian = (i * angleStep) * Mathf.Deg2Rad;
            float x = Mathf.Cos(radian) * orbitRadius;
            float z = Mathf.Sin(radian) * orbitRadius;

            // 타겟을 중심으로 한 궤도 상의 특정 점을 계산합니다.
            Vector3 currentPoint = targetStar.position + new Vector3(x, 0f, z);

            // 첫 번째 점이 계산된 이후부터 이전 점과 현재 점 사이를 잇는 선을 그립니다.
            if (i > 0)
            {
                Gizmos.DrawLine(previousPoint, currentPoint);
            }
            previousPoint = currentPoint;
        }
    }
}