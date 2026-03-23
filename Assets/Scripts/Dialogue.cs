using System.Collections;
using UnityEngine;
using TMPro;

public class Dialogue : MonoBehaviour
{
    [System.Serializable]
    public class DialogueEntry
    {
        [TextArea(3, 10)]
        public string sentence;
        public float duration;
    }

    [Header("참조")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typeSound;

    [Header("대화 설정")]
    [SerializeField] private float startDelay = 2.0f;
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] [Range(0f, 0.5f)] private float pitchRange = 0.1f;
    [SerializeField] private DialogueEntry[] dialogueEntries;
    [SerializeField] private string keyHighlightColor = "#50FF50";

    private int currentIndex = 0;
    // 1. 클래스 상단 변수 선언부에 아래 줄을 추가합니다.
    [HideInInspector] public bool isGamepadMode = false;

    void Start()
    {
        dialogueText.text = "";
        // 기존: StartCoroutine(PlayDialogueRoutine()); 
        // 변경: 시작하자마자 대화를 틀지 않고, 컨트롤러 선택을 기다립니다.
    }

    // 외부(UI 버튼)에서 컨트롤러 선택이 끝나면 이 함수를 호출합니다.
    public void StartDialogue()
    {
        StartCoroutine(PlayDialogueRoutine());
    }

    private IEnumerator PlayDialogueRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        while (currentIndex < dialogueEntries.Length)
        {
            string processedSentence = ProcessTags(dialogueEntries[currentIndex].sentence);
            
            yield return StartCoroutine(TypeText(processedSentence));
            yield return new WaitForSeconds(dialogueEntries[currentIndex].duration);

            currentIndex++;
            dialogueText.text = "";
        }
    }

    private string ProcessTags(string originalText)
    {
        // GameManager에서 가져오던 로직을 지우고, UI에서 받은 변수를 그대로 사용합니다.
        bool isGamepad = isGamepadMode;

        string lHand = isGamepad ? "[LB]" : "[Q]";
        string rHand = isGamepad ? "[RB]" : "[E]";
        string lFoot = isGamepad ? "[LT]" : "[A]";
        string rFoot = isGamepad ? "[RT]" : "[D]";

        string grab = isGamepad ? "[왼쪽 조이스틱]" : "[마우스]";
        string andText = isGamepad ? "은" : "를";

        string result = originalText;
        result = result.Replace("{LH}", $"<color={keyHighlightColor}><b>{lHand}</b></color>");
        result = result.Replace("{RH}", $"<color={keyHighlightColor}><b>{rHand}</b></color>");
        result = result.Replace("{LF}", $"<color={keyHighlightColor}><b>{lFoot}</b></color>");
        result = result.Replace("{RF}", $"<color={keyHighlightColor}><b>{rFoot}</b></color>");

        result = result.Replace("{JOY}", $"<color={keyHighlightColor}><b>{grab}</b></color>" + andText);   
            return result;
    }

    private IEnumerator TypeText(string targetText)
    {
        int charIndex = 0;
        dialogueText.text = "";

        while (charIndex < targetText.Length)
        {
            int randomStep = Random.Range(1, 4);
            int charsAddedThisStep = 0;

            while (charsAddedThisStep < randomStep && charIndex < targetText.Length)
            {
                if (targetText[charIndex] == '<')
                {
                    int tagEndIndex = targetText.IndexOf('>', charIndex);
                    if (tagEndIndex != -1)
                    {
                        string fullTag = targetText.Substring(charIndex, tagEndIndex - charIndex + 1);
                        dialogueText.text += fullTag;
                        charIndex = tagEndIndex + 1;
                        continue; 
                    }
                }

                dialogueText.text += targetText[charIndex];
                charIndex++;
                charsAddedThisStep++;
            }

            if (charsAddedThisStep > 0 && audioSource != null && typeSound != null)
            {
                audioSource.pitch = Random.Range(1f - pitchRange, 1f + pitchRange);
                audioSource.PlayOneShot(typeSound);
            }

            yield return new WaitForSeconds(typingSpeed);
        }
    }
}