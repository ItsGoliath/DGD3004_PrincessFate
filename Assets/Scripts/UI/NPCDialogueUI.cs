using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/// <summary>
/// Basit diyalog paneli: satirlari sira ile gosterir, harf harf yazar; tik/E ile ya satiri tamamlar ya da sonraki satira gecer.
/// maxVisibleCharacters kullanilarak layout sabit tutulur, kelimeler satir sonu kaymaz.
/// </summary>
public class NPCDialogueUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Diyalog Metinleri")]
    [TextArea]
    [SerializeField] private string[] lines;

    [Header("Input")]
    [SerializeField] private InputActionReference advanceAction; // sol tik vb.

    [Header("Yazi Efekti")]
    [SerializeField, Range(0.005f, 0.1f)] private float charDelay = 0.03f;

    [Header("Events")]
    [SerializeField] private UnityEvent onDialogueEnded;

    private int currentIndex;
    private bool isActive;
    private bool isTyping;
    private Coroutine typingRoutine;

    private void Awake()
    {
        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        advanceAction?.action?.Enable();
    }

    private void OnDisable()
    {
        advanceAction?.action?.Disable();
    }

    private void Update()
    {
        if (!isActive)
            return;

        if (AdvancePressed())
        {
            if (isTyping)
            {
                FinishCurrentLineImmediate();
            }
            else
            {
                ShowNext();
            }
        }
    }

    public void StartDialogue()
    {
        if (lines == null || lines.Length == 0 || dialogueText == null)
            return;

        currentIndex = 0;
        isActive = true;

        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(true);

        ShowCurrent();
    }

    private void ShowNext()
    {
        currentIndex++;
        if (currentIndex >= lines.Length)
        {
            EndDialogue();
            return;
        }
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        if (dialogueText == null || currentIndex < 0 || currentIndex >= lines.Length)
            return;

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypeLine(lines[currentIndex]));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = line;
        dialogueText.ForceMeshUpdate();
        int totalChars = dialogueText.textInfo.characterCount;
        dialogueText.maxVisibleCharacters = 0;

        if (totalChars == 0)
        {
            isTyping = false;
            typingRoutine = null;
            yield break;
        }

        float delay = Mathf.Max(0.001f, charDelay);
        for (int i = 0; i < totalChars; i++)
        {
            dialogueText.maxVisibleCharacters = i + 1;
            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
        typingRoutine = null;
    }

    private void FinishCurrentLineImmediate()
    {
        if (dialogueText == null || currentIndex < 0 || currentIndex >= lines.Length)
            return;

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        dialogueText.text = lines[currentIndex];
        dialogueText.ForceMeshUpdate();
        dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
        isTyping = false;
        typingRoutine = null;
    }

    public void EndDialogue()
    {
        isActive = false;
        isTyping = false;
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(false);

        onDialogueEnded?.Invoke();
    }

    // Koddan dinleyici eklemek / cikarmak icin yardimci metotlar.
    public void AddOnDialogueEndedListener(UnityAction action)
    {
        if (action != null)
            onDialogueEnded.AddListener(action);
    }

    public void RemoveOnDialogueEndedListener(UnityAction action)
    {
        if (action != null)
            onDialogueEnded.RemoveListener(action);
    }

    private bool AdvancePressed()
    {
        if (advanceAction != null && advanceAction.action != null && advanceAction.action.triggered)
            return true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            return true;

        return false;
    }
}
