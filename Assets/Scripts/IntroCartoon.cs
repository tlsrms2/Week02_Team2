using UnityEngine;

public class IntroCartoon : MonoBehaviour
{
    [Header("카툰 이미지 배열")]
    [Tooltip("인스펙터에서 순서대로 보여줄 카툰 오브젝트들을 할당해주세요.")]
    public GameObject[] cartoons;

    private int currentIndex = 0; // 현재 보여줄 카툰의 인덱스

    void Start()
    {
        // 씬이 시작될 때 모든 카툰 이미지를 비활성화(숨김) 상태로 만듭니다.
        for (int i = 0; i < cartoons.Length; i++)
        {
            if (cartoons[i] != null)
            {
                cartoons[i].SetActive(false);
            }
        }
    }

    // UI 버튼의 OnClick 이벤트에 연결할 함수입니다.
    public void ShowNextCartoon()
    {
        // 아직 보여줄 카툰이 남아있다면
        if (currentIndex < cartoons.Length)
        {
            if (cartoons[currentIndex] != null)
            {
                cartoons[currentIndex].SetActive(true); // 현재 순서의 카툰 활성화(생성 연출)
            }
            currentIndex++; // 다음 인덱스로 이동
        }
        else
        {
            // 모든 카툰을 다 보았다면 다음 씬(Stage01)으로 넘어갑니다.
            GameManager.Instance.LoadNextScene();
        }
    }
}
