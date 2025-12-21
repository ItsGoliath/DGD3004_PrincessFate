using UnityEngine;

/// <summary>
/// Renkli pad veya buton; baðlý KeypadController (Color mod) ya da ColorPadController'a kimlik gönderir.
/// Basýlýnca hafifçe aþaðý/ileri kayarak basma hissi verir, doðru giriþte basýlý kalýr; reset durumunda Release çaðrýsý ile eski haline döner.
/// Basýlýyken tekrar etkileþimi engeller.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ColorPadButton : MonoBehaviour, IInteractable
{
    [Tooltip("Bu butonun kimliði. Örn: R, G, B, Y.")]
    [SerializeField] private string colorId = "R";
    [SerializeField] private KeypadController keypadController;
    [SerializeField] private ColorPadController legacyController;

    [Header("Görsel Basma")]
    [SerializeField] private Vector3 pressOffset = new Vector3(-0.06f, 0f, 0f);

    private Vector3 originalLocalPos;
    private bool isPressed;

    private void Awake()
    {
        originalLocalPos = transform.localPosition;
    }

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = false; // raycast için fiziksel olsun
    }

    private void OnDisable()
    {
        Release();
    }

    public void Press()
    {
        if (isPressed)
            return;

        isPressed = true;
        transform.localPosition = originalLocalPos + pressOffset;

        if (keypadController != null)
        {
            keypadController.SubmitColor(colorId);
        }
        else if (legacyController != null)
        {
            legacyController.Submit(colorId);
        }
    }

    public void Release()
    {
        transform.localPosition = originalLocalPos;
        isPressed = false;
    }

    public bool IsPressed => isPressed;
    public bool HasController => keypadController != null || legacyController != null;

    // IInteractable
    public bool CanInteract(GameObject interactor)
    {
        if (isPressed)
            return false;
        if (keypadController != null)
        {
            if (keypadController.IsColorSolved)
                return false;
            if (keypadController.RequireFocus && !keypadController.HasFocus)
                return false;
        }
        if (legacyController != null)
        {
            if (legacyController.RequireFocus && !legacyController.HasFocus)
                return false;
        }
        return HasController;
    }

    public void Interact(GameObject interactor)
    {
        Press();
    }

    public void Highlight(bool on, GameObject interactor)
    {
        // özel highlight yok
    }
}
