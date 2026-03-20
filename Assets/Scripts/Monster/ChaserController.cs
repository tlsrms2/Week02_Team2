using System.Collections;
using UnityEngine;

public class ChaserController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Movement")]
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 15f;

    [Header("Trigger")]
    [SerializeField] private float triggerY = 10f;
    [SerializeField] private string animTriggerName = "Attack";

    [Header("Material")]
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material glitchMaterial;

    [Header("Glitch Duration")]
    [SerializeField] private float glitchDuration = 1.5f;

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
        // 글리치 머티리얼로 교체
        _spriteRenderer.material = glitchMaterial;

        // 글리치 지속
        yield return new WaitForSeconds(glitchDuration);

        // 애니메이션 트리거
        if (_animator != null)
            _animator.SetTrigger(animTriggerName);

        yield return new WaitForSeconds(0.1f);

        // 기본 머티리얼 복귀
        _spriteRenderer.material = defaultMaterial;
    }

    private void GameOver()
    {
        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);
    }
}