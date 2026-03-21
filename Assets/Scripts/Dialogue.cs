using System.Collections;
using UnityEngine;
using TMPro;

public class Dialogue : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private TextMeshProUGUI dialogueText; // TMP 컴포넌트 연결

    [Header("설정")]
    [TextArea(3, 10)]
    [SerializeField] private string[] sentences;       // 출력할 문장 배열
    [SerializeField] private float typingSpeed = 0.05f; // 글자당 출력 속도
    [SerializeField] private float displayDuration = 2.0f; // 문장 완료 후 대기 시간

    private int currentIndex = 0; // 현재 문장 번호

    void Start()
    {
        // 텍스트 초기화 후 시작
        dialogueText.text = "";
        StartCoroutine(PlayDialogueRoutine());
    }

    private IEnumerator PlayDialogueRoutine()
    {
        while (currentIndex < sentences.Length)
        {
            // 1. 한 문장 타이핑 시작
            yield return StartCoroutine(TypeText(sentences[currentIndex]));

            // 2. 문장 출력이 끝나면 잠시 대기 (플레이어는 못 넘김)
            yield return new WaitForSeconds(displayDuration);

            // 3. 다음 문장으로 인덱스 증가 및 텍스트 초기화
            currentIndex++;
            dialogueText.text = "";
        }

        // 모든 대화가 끝나면 다음 연출(예: 씬 전환)을 실행합니다.
        EndDialogue();
    }

    private IEnumerator TypeText(string targetText)
    {
        int charIndex = 0;
        
        while (charIndex < targetText.Length)
        {
            // 한 번에 1~3글자씩 랜덤하게 출력 (언더테일 느낌의 불규칙함 추가)
            int randomStep = Random.Range(1, 4); 
            
            for (int i = 0; i < randomStep; i++)
            {
                if (charIndex < targetText.Length)
                {
                    dialogueText.text += targetText[charIndex];
                    charIndex++;
                }
            }

            // 타자 소리 등을 여기서 재생하면 더욱 좋습니다.
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    private void EndDialogue()
    {
        Debug.Log("모든 튜토리얼 대화 종료. 게임 속으로 진입하는 연출을 시작합니다.");
        // GameManager.Instance.LoadNextScene(); 등을 호출하여 전환할 수 있습니다.
    }
}