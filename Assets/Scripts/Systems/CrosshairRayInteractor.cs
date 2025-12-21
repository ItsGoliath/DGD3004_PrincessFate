using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// IInteractable hedefleri raycast ile tespit eder ve crosshair'i yönetir.
/// FPS modunda ekran merkezinden ray; screenPointMode açýkken mouse pozisyonunu kullanýr.
/// </summary>
public class CrosshairRayInteractor : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera cam;
    [SerializeField] private bool autoRefreshCamera = false; // Camera.main deðiþirse otomatik çek
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private bool useViewportRay = true; // FPS modunda 0.5/0.5
    [SerializeField] private string[] ignoreHitTags; // Bu tag'deki collider'lar yok sayýlýr (örn: Proximity)

    [Header("Crosshair UI")]
    [SerializeField] private RectTransform crosshair;
    [SerializeField] private Image crosshairImage;
    [SerializeField] private bool enableHighlight = true;
    [SerializeField] private Vector2 normalScale = Vector2.one;
    [SerializeField] private Vector2 highlightScale = new Vector2(1.2f, 1.2f);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.cyan;

    [Header("Debug")]
    [SerializeField] private bool debugDrawRay = false;
    [SerializeField] private Color debugRayColor = Color.yellow;
    [SerializeField] private float debugRayDuration = 0.02f;

    private IInteractable current;
    private bool screenPointMode;

    public void SetScreenPointMode(bool on)
    {
        screenPointMode = on;
        if (!on)
            ResetCrosshairToCenter();
    }

    public void SetCrosshair(RectTransform rt, Image img)
    {
        crosshair = rt;
        crosshairImage = img;
    }

    public void SetCamera(Camera targetCamera)
    {
        cam = targetCamera;
    }

    private void OnEnable()
    {
        interactAction?.action?.Enable();
        SetCrosshairState(false);
    }

    private void OnDisable()
    {
        interactAction?.action?.Disable();
        ClearHighlight();
        SetCrosshairState(false);
        screenPointMode = false;
    }

    private void Update()
    {
        if (cam == null || autoRefreshCamera)
            cam = Camera.main;
        if (cam == null)
        {
            SetCrosshairState(false);
            return;
        }

        Vector3 screenPt = GetScreenPoint();
        Ray ray = BuildRay(screenPt);
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, interactDistance, interactMask, QueryTriggerInteraction.Collide);

        if (hit && ShouldIgnore(hitInfo.collider))
        {
            hit = false;
        }

        if (debugDrawRay)
            Debug.DrawRay(ray.origin, ray.direction * interactDistance, debugRayColor, debugRayDuration);

        UpdateCrosshairPosition(screenPt);

        IInteractable found = null;
        if (hit)
            found = hitInfo.collider.GetComponent<IInteractable>() ?? hitInfo.collider.GetComponentInParent<IInteractable>() ?? hitInfo.collider.GetComponentInChildren<IInteractable>();

        bool canInteract = found != null && found.CanInteract(gameObject);

        if (canInteract)
        {
            if (current != found)
            {
                ClearHighlight();
                current = found;
            }
            current.Highlight(true, gameObject);
        }
        else
        {
            ClearHighlight();
        }

        SetCrosshairState(canInteract);

        if (canInteract && InteractPressedThisFrame())
        {
            current?.Interact(gameObject);
            SetCrosshairState(false);
        }
    }

    private Ray BuildRay(Vector3 screenPt)
    {
        if (!screenPointMode && useViewportRay)
            return cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        return cam.ScreenPointToRay(screenPt);
    }

    private Vector3 GetScreenPoint()
    {
        if (!screenPointMode)
            return new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        return new Vector3(Mathf.Clamp(mousePos.x, 0, Screen.width), Mathf.Clamp(mousePos.y, 0, Screen.height), 0f);
    }

    private void UpdateCrosshairPosition(Vector3 screenPt)
    {
        if (crosshair == null)
            return;

        if (!screenPointMode)
        {
            crosshair.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            return;
        }

        crosshair.position = screenPt;
    }

    private void ClearHighlight()
    {
        if (current != null)
            current.Highlight(false, gameObject);
        current = null;
    }

    private bool InteractPressedThisFrame()
    {
        if (interactAction != null && interactAction.action != null)
            return interactAction.action.triggered;

        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
            return true;

        var gp = Gamepad.current;
        if (gp != null && gp.buttonWest.wasPressedThisFrame)
            return true;

        return Input.GetKeyDown(KeyCode.E);
    }

    private void SetCrosshairState(bool highlighted)
    {
        if (crosshair != null && enableHighlight)
            crosshair.localScale = highlighted ? highlightScale : normalScale;

        if (crosshairImage != null && enableHighlight)
            crosshairImage.color = highlighted ? highlightColor : normalColor;
    }

    private void ResetCrosshairToCenter()
    {
        if (crosshair == null)
            return;
        crosshair.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
    }

    private bool ShouldIgnore(Collider col)
    {
        if (ignoreHitTags == null || ignoreHitTags.Length == 0)
            return false;
        for (int i = 0; i < ignoreHitTags.Length; i++)
        {
            var t = ignoreHitTags[i];
            if (!string.IsNullOrEmpty(t) && col.CompareTag(t))
                return true;
        }
        return false;
    }
}
