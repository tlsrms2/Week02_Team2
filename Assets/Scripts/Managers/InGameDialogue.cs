using System.Collections;
using TMPro;
using UnityEngine;

public class InGameDialogue : MonoBehaviour
{
    [Header("대사 설정")]
    [TextArea(3, 5)]
    public string sentence; // 출력할 대사
    public float typeSpeed = 0.05f; // 타이핑 속도
    public float displayDuration = 3f; // 출력 후 유지되는 시간

    [Header("UI 연결")]
    public TextMeshProUGUI dialogueText; // 텍스트를 표시할 UI

    [Header("사운드 설정")]
    public AudioSource typeAudioSource;
    public AudioClip typeSound;
    [Range(0f, 0.5f)] public float pitchRange = 0.1f;

    [Header("충돌 설정")]
    [Tooltip("충돌을 감지할 대상의 레이어를 선택하세요.")]
    public LayerMask targetLayer; 
    public bool isTrigger = true; // Trigger 방식인지 Collision 방식인지 구분
    public bool playOnlyOnce = true; // 한 번만 실행할 것인지 여부

    private bool hasPlayed = false;
    private Coroutine typingCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (!isTrigger) return;

        // 충돌한 오브젝트의 레이어가 targetLayer에 포함되어 있는지 확인
        if (((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            TriggerDialogue();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isTrigger) return;

        if (((1 << collision.gameObject.layer) & targetLayer) != 0)
        {
            TriggerDialogue();
        }
    }

    private void TriggerDialogue()
    {
        if (playOnlyOnce && hasPlayed) return;

        hasPlayed = true;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeAndClearRoutine());
    }

    private IEnumerator TypeAndClearRoutine()
    {
        dialogueText.text = "";
        int charCount = 0;

        foreach (char letter in sentence)
        {
            dialogueText.text += letter;
            charCount++;
            
            // 공백이 아닐 때, 3글자마다 한 번씩 소리 재생 (자연스러운 타자 소리)
            if (letter != ' ')
            {
                PlayTypingSound();
            }

            yield return new WaitForSeconds(typeSpeed);
        }

        // 다 출력한 뒤 일정 시간 동안 유지
        yield return new WaitForSeconds(displayDuration);

        // 텍스트 지우기
        dialogueText.text = "";
        typingCoroutine = null;
    }

    private void PlayTypingSound()
    {
        if (typeAudioSource != null && typeSound != null)
        {
            typeAudioSource.pitch = Random.Range(1f - pitchRange, 1f + pitchRange);
            typeAudioSource.PlayOneShot(typeSound);
        }
    }
}
