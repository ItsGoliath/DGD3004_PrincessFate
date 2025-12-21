using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2D sahnede sistem imlecini gizleyip yerine bir sprite/transform'u pointer konumuna tasir.
/// Focus disindayken SetCursorActive(false) ile kilitle.
/// </summary>
public class LevelCursor2D : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform cursorVisual;
    [SerializeField] private float cursorPlaneZ = 0f;
    [SerializeField] private bool hideSystemCursor = true;
    [SerializeField] private bool manageCursor = true;
    [SerializeField] private bool allowMovement = false; // varsayilan kilitli
    [SerializeField] private bool hideVisualWhenInactive = true;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference pointAction;

    void OnEnable()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (cursorVisual == null)
            cursorVisual = transform;

        pointAction?.action?.Enable();
        ApplyCursorState(allowMovement);
    }

    void OnDisable()
    {
        pointAction?.action?.Disable();
    }

    void Update()
    {
        if (!allowMovement || targetCamera == null || cursorVisual == null)
            return;

        Vector2 screen2D = GetPointerPosition();
        Vector3 screen = new Vector3(screen2D.x, screen2D.y, 0f);
        float depth = Mathf.Abs(targetCamera.transform.position.z - cursorPlaneZ);
        screen.z = depth;

        Vector3 world = targetCamera.ScreenToWorldPoint(screen);
        world.z = cursorPlaneZ;
        cursorVisual.position = world;
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

    /// <summary>Cursor hareketini ac/kapat ve gerekirse sistem imlecini goster.</summary>
    public void SetCursorActive(bool active)
    {
        allowMovement = active;
        ApplyCursorState(active);
    }

    private void ApplyCursorState(bool active)
    {
        if (cursorVisual != null && hideVisualWhenInactive)
            cursorVisual.gameObject.SetActive(active);

        if (manageCursor && hideSystemCursor)
        {
            Cursor.visible = active ? false : true;
            Cursor.lockState = active ? CursorLockMode.Confined : CursorLockMode.None;
        }

        if (pointAction != null && pointAction.action != null)
        {
            if (active) pointAction.action.Enable();
            else pointAction.action.Disable();
        }
    }
}
