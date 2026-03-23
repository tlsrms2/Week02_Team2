using System.Collections;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class ChaserController : MonoBehaviour
{
    public enum BossType
    {
        None,
        PackMan,
        DonkeyKong
    }
    [Header("State")]
    [SerializeField] private Transform player;
    [SerializeField] bool isChasing = false;
    [SerializeField] BossType bossType; 
    [Header("Follow")]
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 15f;

    [Header("Trigger")]
    [SerializeField] private float triggerY = 10f;

    [Header("Directing")]
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material glitchMaterial;
    [SerializeField] float duration;
    [SerializeField] Transform startPoint;

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
        _spriteRenderer.material = glitchMaterial;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
            Debug.LogError("ChaserController: Player�� ã�� ���߽��ϴ�!");
    }

    void Update()
    {
        if(!isChasing) return; 
        if (player == null) return;
        if (bossType == BossType.DonkeyKong)
        {
            HandleChaseUp();
            return;
        }
        HandleChase();
        HandleLookAt();
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

    private void HandleChaseUp()
    {
        float distanceY = player.position.y - transform.position.y;
        float speed = Mathf.Lerp(minSpeed, maxSpeed,
            Mathf.InverseLerp(minDistance, maxDistance, Mathf.Abs(distanceY)));

        Vector3 target = new Vector3(transform.position.x, player.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
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

    public void SetChasing(bool value)
    {
        isChasing = value;
    }
    public void Init()
    {
        transform.position = startPoint.position;
        isChasing = true;
    }

    public void StartGlitchFade()
    {
        StartCoroutine(GlitchFadeCoroutine());
        SetGlitch();
    }
    public void SetGlitch() { 

        _spriteRenderer.material.SetFloat("_GlitchIntensity", 0);
        _spriteRenderer.material = defaultMaterial;
    }
    private IEnumerator GlitchFadeCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float value = Mathf.Lerp(1f, 0f, t);
            _spriteRenderer.material.SetFloat("_GlitchIntensity", value);

            yield return null;
        }

        _spriteRenderer.material.SetFloat("_GlitchIntensity", 0f);
        _spriteRenderer.material = defaultMaterial;
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

        SoundManager.Instance.PlayGameOverBgm();
        // InGameManager�� ���ӿ��� �˸�
        InGameManager inGameManager = FindAnyObjectByType<InGameManager>();
        if (inGameManager != null)
            inGameManager.SetGameOver();
    }


}