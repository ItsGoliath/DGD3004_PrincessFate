using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// UI içindeki bir imleci (RectTransform) pointer pozisyonuna taþýr.
/// Ýsterseniz Locked cursor + sanal imleç moduyla ekrandan taþmadan serbest dolaþabilir.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UICursorFollower : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private Canvas canvas;
    [Header("Locked Cursor (Sanal Imleç)")]
    [SerializeField] private bool useLockedCursor = false;
    [SerializeField] private float lockedCursorSensitivity = 1f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference pointAction;
    [SerializeField] private InputActionReference deltaAction;

    private Vector2 virtualScreenPos;
    private bool initialized;

    void Reset()
    {
        target = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        InitVirtualPos();
    }

    void OnEnable()
    {
        pointAction?.action?.Enable();
        deltaAction?.action?.Enable();
    }

    void OnDisable()
    {
        pointAction?.action?.Disable();
        deltaAction?.action?.Disable();
    }

    void Update()
    {
        if (target == null || canvas == null)
            return;

        if (useLockedCursor)
        {
            FollowWithLockedCursor();
        }
        else
        {
            FollowSystemCursor();
        }
    }

    private void FollowSystemCursor()
    {
        Vector2 pointerPos = GetPointerPosition();

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            target.anchoredPosition = pointerPos;
        }
        else
        {
            // Kamera tabanlý canvas'ta ekran noktasý kamera rect'inin dýþýndaysa uyarý almamak için çevrimi yapma.
            if (canvas.worldCamera != null && !canvas.worldCamera.pixelRect.Contains(pointerPos))
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                pointerPos,
                canvas.worldCamera,
                out Vector2 localPoint);
            target.anchoredPosition = localPoint;
        }
    }

    private void FollowWithLockedCursor()
    {
        if (!initialized)
            InitVirtualPos();

        Vector2 delta = GetPointerDelta() * lockedCursorSensitivity;
        virtualScreenPos += delta;

        float maxX = Screen.width;
        float maxY = Screen.height;
        virtualScreenPos.x = Mathf.Clamp(virtualScreenPos.x, 0f, maxX);
        virtualScreenPos.y = Mathf.Clamp(virtualScreenPos.y, 0f, maxY);

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            target.anchoredPosition = virtualScreenPos;
        }
        else
        {
            // Kamera tabanlý canvas'ta ekran noktasý kamera rect'inin dýþýndaysa uyarý almamak için çevrimi yapma.
            if (canvas.worldCamera != null && !canvas.worldCamera.pixelRect.Contains(virtualScreenPos))
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                virtualScreenPos,
                canvas.worldCamera,
                out Vector2 localPoint);
            target.anchoredPosition = localPoint;
        }
    }

    private void InitVirtualPos()
    {
        virtualScreenPos = GetPointerPosition();
        initialized = true;
    }

    private Vector2 GetPointerPosition()
    {
        if (pointAction != null && pointAction.action != null)
            return pointAction.action.ReadValue<Vector2>();

        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        if (Pointer.current != null)
            return Pointer.current.position.ReadValue();

        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private Vector2 GetPointerDelta()
    {
        if (deltaAction != null && deltaAction.action != null)
            return deltaAction.action.ReadValue<Vector2>();

        if (Mouse.current != null)
            return Mouse.current.delta.ReadValue();

        if (Pointer.current != null)
            return Pointer.current.delta.ReadValue();

        return Vector2.zero;
    }
}
