using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2D NPC etkileþimi: Player tetikleyip E (veya aksiyon) bastýðýnda diyalog panelini açar,
/// isteðe baðlý player kontrollerini kapatýr. Diyalog bitince kontrolleri açar;
/// oyuncu X eþiðinin altýna geçtiðinde NPC görselini deðiþtirir ve istenirse etkileþimi kilitler.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NPCDialogueTrigger : MonoBehaviour
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
    [SerializeField] private bool waitForPlayerXThreshold = true;
    [SerializeField] private float playerXThreshold = 56f;

    [Header("Etkileþim Kilidi")]
    [SerializeField] private bool disableFurtherInteraction = false;  // diyalog tamamlanýnca direkt kilitle
    [SerializeField] private bool lockAfterThreshold = true;          // oyuncu X eþiðinin altýna geçince kilitle

    private bool playerInside;
    private bool dialogueCompleted;
    private bool visualsApplied;
    private bool interactionLocked;
    private Transform playerTransform;

    public bool HasDialogueCompleted => dialogueCompleted;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnEnable()
    {
        interactAction?.action?.Enable();
        if (dialogueUI != null)
            dialogueUI.AddOnDialogueEndedListener(OnDialogueEnded);
    }

    private void OnDisable()
    {
        if (dialogueUI != null)
            dialogueUI.RemoveOnDialogueEndedListener(OnDialogueEnded);
        interactAction?.action?.Disable();
    }

    private void Update()
    {
        // Diyalog bitti, görsel hâlâ deðiþmediyse ve oyuncu X eþiðinin altýna düþtüyse uygula.
        if (dialogueCompleted && !visualsApplied && waitForPlayerXThreshold && playerTransform != null)
        {
            if (playerTransform.position.x < playerXThreshold)
                ApplyVisualSwap();
        }

        // Diyalog bitti, X eþiði geçildiyse etkileþimi kilitle (tekrar oynanmasýn).
        if (!interactionLocked && dialogueCompleted && lockAfterThreshold && playerTransform != null)
        {
            if (playerTransform.position.x < playerXThreshold)
                interactionLocked = true;
        }

        if (interactionLocked)
            return;

        if (dialogueCompleted && disableFurtherInteraction)
            return;

        if (requirePlayerInside && !playerInside)
            return;

        if (InteractPressed())
            OpenDialogue();
    }

    private void OpenDialogue()
    {
        if (interactionLocked)
            return;

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
    /// Diyalog bittikten sonra çaðrýlýr. Kontrolleri geri açar; gerekirse görsel deðiþimi için oyuncu X eþiðini bekler.
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

        if (!waitForPlayerXThreshold)
        {
            ApplyVisualSwap();
        }
        else if (playerTransform != null && playerTransform.position.x < playerXThreshold)
        {
            ApplyVisualSwap();
        }
        // Aksi hâlde Update içinde X < threshold olunca uygulanacak.
    }

    private void ApplyVisualSwap()
    {
        visualsApplied = true;

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
        if (other.CompareTag(playerTag))
        {
            playerInside = true;
            if (playerTransform == null)
                playerTransform = other.transform;

            // Diyalog bitmiþ, X eþiði saðlanmýþsa anýnda uygula/kilitle
            if (dialogueCompleted && playerTransform.position.x < playerXThreshold)
            {
                if (waitForPlayerXThreshold && !visualsApplied)
                    ApplyVisualSwap();

                if (lockAfterThreshold)
                    interactionLocked = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!requirePlayerInside)
            return;

        if (other.CompareTag(playerTag))
            playerInside = false;
    }
}
