using System.Collections;
using UnityEngine;

public class CameraFade : MonoBehaviour
{
    public static CameraFade Instance;

    private float _alpha = 0f;
    private bool _isDrawing = false;

    void Awake()
    {
        Instance = this;
    }

    // 외부에서 호출
    public IEnumerator FadeToBlack(float duration)
    {
        _alpha = 0f;
        _isDrawing = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        _alpha = 1f;
    }

    public IEnumerator FadeFromBlack(float duration)
    {
        _alpha = 1f;
        _isDrawing = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        _alpha = 0f;
        _isDrawing = false;
    }

    // 카메라 렌더링 후 검정 오버레이 직접 그림
    void OnPostRender()
    {
        if (!_isDrawing || _alpha <= 0f) return;

        GL.PushMatrix();
        GL.LoadOrtho();

        GL.Begin(GL.QUADS);
        GL.Color(new Color(0f, 0f, 0f, _alpha));
        GL.Vertex3(0f, 0f, -1f);
        GL.Vertex3(0f, 1f, -1f);
        GL.Vertex3(1f, 1f, -1f);
        GL.Vertex3(1f, 0f, -1f);
        GL.End();

        GL.PopMatrix();
    }
}