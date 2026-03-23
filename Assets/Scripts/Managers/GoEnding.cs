using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GoEnding : MonoBehaviour
{
    [Header("UI References")]
    public Image fadeImage; // 에디터에서 FadeImage를 할당하세요.

    [Header("Settings")]
    public float fadeDuration = 1.0f; // 페이드에 걸리는 시간 (초)

    public GameObject Monster;

    private bool isFading = false;

    private void Awake()
    {

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Monster.GetComponent<BoxCollider>().enabled = false;
            LoadSceneWithFade("Ending");
        }
    }

    public void LoadSceneWithFade(string sceneName)
    {
        if (isFading) return; // 이미 페이드 중이면 중복 실행 방지
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    // 화면이 어두워지고 씬을 로드하는 코루틴 (투명 -> 검정)
    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        isFading = true;
        fadeImage.gameObject.SetActive(true); // 클릭 방지를 위해 킴

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // 알파값을 0에서 1로 서서히 늘림
            SetImageAlpha(timer / fadeDuration);
            yield return null;
        }

        SetImageAlpha(1f); // 확실하게 검은색으로 설정

        // 페이드 아웃이 끝난 후 씬 로드
        SceneManager.LoadScene(sceneName);
    }

    // 이미지의 알파값만 변경해주는 편의 함수
    private void SetImageAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = Mathf.Clamp01(alpha); // 0~1 사이 값으로 제한
            fadeImage.color = color;
        }
    }

    // 화면이 밝아지는 코루틴 (투명 -> 검정)
    private IEnumerator FadeIn()
    {
        isFading = true;
        fadeImage.gameObject.SetActive(true); // 혹시 꺼져있다면 킴

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // 알파값을 1에서 0으로 서서히 줄임
            SetImageAlpha(1f - (timer / fadeDuration));
            yield return null;
        }

        SetImageAlpha(0f); // 확실하게 투명하게 설정
        fadeImage.gameObject.SetActive(false); // 클릭 방지 해제를 위해 끔
        isFading = false;
    }
}
