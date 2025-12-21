using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Keypad panel: hem sayýsal kod hem renk dizisi modunu destekler.
/// Sayýsal modda UI açýlýr, kod yazýlýr; renk modunda butonlar SubmitColor ile tetikler.
/// </summary>
public class KeypadController : MonoBehaviour
{
    public enum KeypadMode
    {
        Numeric,
        Color
    }

    [Header("Mod")]
    [SerializeField] private KeypadMode mode = KeypadMode.Numeric;

    [Header("Input Actions (Numeric)")]
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private string playerTag = "Player3D";

    [Header("Focus")]
    [SerializeField] private bool requireFocus = true;
    private bool hasFocus;
    public bool HasFocus => !requireFocus || hasFocus;
    public bool RequireFocus => requireFocus;

    [Header("Kod Ayarlarý (Numeric)")]
    [SerializeField] private string correctCode = "1234";
    [SerializeField] public TMP_InputField inputField;
    [SerializeField] private int maxDigits = 4;

    [Header("Renk Dizisi (Color)")]
    [Tooltip("Doðru renk sýrasý (örn: R, G, B, Y)")]
    [SerializeField] private string[] colorSequence = new string[] { "R", "G", "B", "Y" };
    [Tooltip("Reset durumda serbest býrakýlacak tüm renk butonlarý (basýlý pozdan eski haline döner). Boþsa çocuklardan otomatik bulur.")]
    [SerializeField] private ColorPadButton[] colorButtons;

    [Header("UI")]
    [SerializeField] private GameObject keypadUI;
    [SerializeField] private bool closeOnDisable = true;
    [SerializeField] private bool closeOnTriggerExit = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onCorrectCode;
    [SerializeField] private UnityEvent onWrongCode;

    [Header("Feedback (Numeric)")]
    [SerializeField] private string successMessage = "Congrats!";
    [SerializeField] private TMP_Text feedbackText;

    [Header("Opsiyonel Disk Reveal")]
    [Tooltip("Doðru giriþte disk açýlýþý oynatýlsýn.")]
    [SerializeField] private DiskRevealController diskReveal;
    [SerializeField] private bool playDiskRevealOnCorrect = true;

    private bool playerInside;
    private bool isOpen;
    private int colorIndex;
    private bool colorSolved;
    private bool colorHasError;

    public bool IsColorSolved => colorSolved;

    private void OnEnable()
    {
        interactAction?.action?.Enable();
    }

    private void OnDisable()
    {
        interactAction?.action?.Disable();
        if (mode == KeypadMode.Numeric && closeOnDisable)
            CloseUI();
    }

    private void Update()
    {
        if (mode != KeypadMode.Numeric)
            return;

        if (requireFocus && !HasFocus)
            return;

        if (!playerInside)
            return;

        if (InteractPressed())
        {
            if (!isOpen)
                OpenUI();
            // ESC ile kapanmasýn istiyorsak burada CloseUI çaðrýsý yok
        }
    }

    public void SetFocus(bool on)
    {
        hasFocus = on;
        if (!on && mode == KeypadMode.Numeric)
            CloseUI();
    }

    #region Numeric
    public void SubmitCode()
    {
        if (mode != KeypadMode.Numeric)
            return;

        string entered = inputField != null ? inputField.text : string.Empty;
        bool ok = string.Equals(entered, correctCode, System.StringComparison.Ordinal);

        if (ok)
        {
            HandleCorrect();
            if (inputField != null)
                inputField.interactable = false; // aktif kalsýn ama giriþ kapansýn
        }
        else
        {
            onWrongCode?.Invoke();
            if (inputField != null)
                inputField.text = string.Empty;
            ShowFeedback(string.Empty);
            Debug.Log("Keypad (Numeric): wrong code");
        }
    }

    private void OpenUI()
    {
        isOpen = true;
        if (keypadUI != null)
            keypadUI.SetActive(true);
        if (inputField != null)
        {
            inputField.text = string.Empty;
            inputField.gameObject.SetActive(true);
            inputField.interactable = true;
            inputField.ActivateInputField();
            inputField.Select();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ShowFeedback(string.Empty);
    }

    private void CloseUI()
    {
        isOpen = false;
        if (keypadUI != null)
            keypadUI.SetActive(false);
        if (EventSystem.current != null && inputField != null && EventSystem.current.currentSelectedGameObject == inputField.gameObject)
            EventSystem.current.SetSelectedGameObject(null);
        ShowFeedback(string.Empty);
    }

    public void AppendDigit(string digit)
    {
        if (mode != KeypadMode.Numeric)
            return;

        if (inputField == null || string.IsNullOrEmpty(digit))
            return;

        if (inputField.text.Length >= maxDigits)
        {
            SubmitCode();
            return;
        }

        inputField.text += digit;

        if (inputField.text.Length >= maxDigits)
        {
            SubmitCode();
        }
    }

    public void ClearInput()
    {
        if (mode != KeypadMode.Numeric)
            return;

        if (inputField != null)
            inputField.text = string.Empty;
        ShowFeedback(string.Empty);
    }

    private bool InteractPressed()
    {
        if (interactAction != null && interactAction.action != null)
            return interactAction.action.triggered;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
            return true;

        Gamepad gp = Gamepad.current;
        if (gp != null && gp.buttonWest.wasPressedThisFrame)
            return true;

        return false;
    }

    private void ShowFeedback(string msg)
    {
        if (feedbackText == null)
            return;

        feedbackText.text = msg;
        feedbackText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
    }
    #endregion

    #region Color
    public void SubmitColor(string colorId)
    {
        if (mode != KeypadMode.Color)
            return;

        if (requireFocus && !HasFocus)
            return;

        if (colorSolved)
            return;

        if (colorSequence == null || colorSequence.Length == 0)
            return;

        string input = string.IsNullOrEmpty(colorId) ? string.Empty : colorId.Trim().ToUpperInvariant();
        string expected = colorSequence[colorIndex].Trim().ToUpperInvariant();

        bool match = input == expected;
        if (!match)
            colorHasError = true;

        colorIndex++;

        Debug.Log($"Keypad (Color) step {colorIndex}/{colorSequence.Length}: {(match ? "CORRECT" : "WRONG")} ({input})");

        if (colorIndex >= colorSequence.Length)
        {
            if (!colorHasError)
            {
                colorSolved = true;
                HandleCorrect();
            }
            else
            {
                Debug.Log("Keypad (Color): sequence filled but incorrect, resetting");
                ResetColorProgress();
                ReleaseColorButtons();
                onWrongCode?.Invoke();
            }
        }
    }

    public void ResetColorProgress()
    {
        colorIndex = 0;
        colorSolved = false;
        colorHasError = false;
    }

    public void ReleaseColorButtons()
    {
        ColorPadButton[] buttons = colorButtons;
        if (buttons == null || buttons.Length == 0)
            buttons = GetComponentsInChildren<ColorPadButton>(true);

        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                buttons[i].Release();
        }
    }
    #endregion

    private void HandleCorrect()
    {
        Debug.Log("Keypad: CORRECT code/sequence");
        onCorrectCode?.Invoke();
        if (KeyItemState.Instance != null)
            KeyItemState.Instance.GrantKeyItem();
        ShowFeedback(successMessage);

        if (playDiskRevealOnCorrect)
        {
            var target = diskReveal != null ? diskReveal : GetComponentInParent<DiskRevealController>();
            if (target != null)
            {
                Debug.Log("Keypad: Playing disk reveal");
                target.PlayReveal();
            }
            else
            {
                Debug.LogWarning("Keypad: DiskRevealController bulunamadý, disk açýlmayacak");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (mode != KeypadMode.Numeric)
            return;

        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
            playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (mode != KeypadMode.Numeric)
            return;

        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
        {
            playerInside = false;
            if (closeOnTriggerExit)
                CloseUI();
        }
    }
}
