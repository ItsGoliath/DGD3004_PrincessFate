using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2D NPC etkileþimi: Player tetikleyip E (veya aksiyon) bastýðýnda diyalog panelini açar,
/// canvas'ý aktif eder, isteðe baðlý olarak player kontrol scriptlerini kapatýr ve diyalog bitince
/// kontrolleri geri açar, NPC görselini deðiþtirebilir ve tekrar etkileþimi kapatabilir.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NPCDialogueActivator : MonoBehaviour
{
    [Header("Temel")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private NPCDialogueUI dialogueUI;
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private InputActionReference interactAction; // E vb.
    [SerializeField] private bool requirePlayerInside = true;

    [Header("Player Kontrol Kapatma")]
    [SerializeField] private MonoBehaviour[] disableOnOpen; // hareket / slash scriptleri

    [Header("Diyalog Sonrasý NPC Görseli")]
    [SerializeField] private GameObject[] visualsToDisable;
    [SerializeField] private GameObject[] visualsToEnable;
    [SerializeField] private bool disableFurtherInteraction = true;

    private bool playerInside;
    private bool dialogueCompleted;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnEnable()
    {
        interactAction?.action?.Enable();
    }

    private void OnDisable()
    {
        interactAction?.action?.Disable();
    }

    private void Update()
    {
        if (dialogueCompleted && disableFurtherInteraction)
            return;

        if (requirePlayerInside && !playerInside)
            return;

        if (InteractPressed())
            OpenDialogue();
    }

    private void OpenDialogue()
    {
        if (dialogueCompleted && disableFurtherInteraction)
            return;

        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(true);

        if (disableOnOpen != null)
        {
            foreach (var mb in disableOnOpen)
            {
                if (mb != null)
                    mb.enabled = false;
            }
        }

        dialogueUI?.StartDialogue();
    }

    /// <summary>
    /// Diyalog bittikten sonra çaðýrýlmalý. Kontrolleri geri açar, NPC görselini deðiþtirir, tekrar etkileþimi kapatabilir.
    /// </summary>
    public void OnDialogueEnded()
    {
        dialogueCompleted = true;

        if (disableOnOpen != null)
        {
            foreach (var mb in disableOnOpen)
            {
                if (mb != null)
                    mb.enabled = true;
            }
        }

        if (visualsToDisable != null)
        {
            foreach (var go in visualsToDisable)
            {
                if (go != null)
                    go.SetActive(false);
            }
        }

        if (visualsToEnable != null)
        {
            foreach (var go in visualsToEnable)
            {
                if (go != null)
                    go.SetActive(true);
            }
        }
    }

    private bool InteractPressed()
    {
        if (interactAction != null && interactAction.action != null && interactAction.action.triggered)
            return true;

        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
            return true;

        var gp = Gamepad.current;
        if (gp != null && gp.buttonWest.wasPressedThisFrame)
            return true;

        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!requirePlayerInside)
            return;

        if (other.CompareTag(playerTag))
            playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!requirePlayerInside)
            return;

        if (other.CompareTag(playerTag))
            playerInside = false;
    }
}
