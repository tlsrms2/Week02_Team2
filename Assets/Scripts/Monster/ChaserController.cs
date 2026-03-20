using System.Collections;
using UnityEngine;

public class ChaserController : MonoBehaviour
{
    [Header("목표")]
    [SerializeField] private Transform player;

    [Header("움직임 힘")]
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 15f;

    [Header("스테이지 클리어 Y")]
    [SerializeField] private float triggerY = 10f;

    [Header("연출 재질")]
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material glitchMaterial;
    [SerializeField] private float glitchIntersity = 0.8f;

    [Header("GameOver")]
    [SerializeField] private GameObject gameOverPanel;

    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "BodyMesh")
            GameOver();
    }

    void Start()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.material = defaultMaterial;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
            Debug.LogError("ChaserController: Player를 찾지 못했습니다!");
    }

    void Update()
    {
        if (player == null) return;

        HandleChase();
        HandleLookAt();
        CheckTrigger();
    }

    private void HandleChase()
    {
        float distance = Vector3.Distance(player.position, transform.position);

        float speed = Mathf.Lerp(minSpeed, maxSpeed,
            Mathf.InverseLerp(minDistance, maxDistance, distance));

        transform.position = Vector3.MoveTowards(
            transform.position,
            player.position,
            speed * Time.deltaTime
        );
    }

    private void HandleLookAt()
    {
        Vector3 dirToPlayer = player.position - transform.position;

        if (dirToPlayer.sqrMagnitude < 0.001f) return;

        float angle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void CheckTrigger()
    {
        if (_triggered) return;

        if (transform.position.y >= triggerY)
        {
            _triggered = true;
            StartCoroutine(GlitchAndTrigger());
        }
    }

    private IEnumerator GlitchAndTrigger()
    {
        _spriteRenderer.material = glitchMaterial;
        _spriteRenderer.material.SetFloat("_GlitchIntensity", 0f);

        float intensity = 0f;

        while (intensity < 0.5f)
        {
            intensity = Mathf.MoveTowards(intensity, 0.5f, 0.3f * Time.deltaTime);
            _spriteRenderer.material.SetFloat("_GlitchIntensity", intensity);
            yield return null;
        }
        _spriteRenderer.material.SetFloat("_GlitchIntensity", 0.5f);
    }

    private void GameOver()
    {
        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);
    }
}