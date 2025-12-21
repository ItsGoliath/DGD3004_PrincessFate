using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Reflection;
using System.Collections.Generic;

/// <summary>
/// Pad/Keypad odaklanmak için vCam önceliğini değiştirir, FPS hareket/bakışı kapatır, crosshair yönetir.
/// </summary>
public class VCamFocusInteract : MonoBehaviour, IInteractable
{
    [System.Serializable]
    private struct VCamPriority
    {
        public Component vcam;
        public int priorityDefault;
        public int priorityWhenFocused;
    }

    [Header("VCam Priority")]
    [SerializeField] private VCamPriority fpsVcam = new VCamPriority { priorityDefault = 10, priorityWhenFocused = 9 };
    [SerializeField] private VCamPriority focusVcam = new VCamPriority { priorityDefault = 9, priorityWhenFocused = 20 };

    [Header("Movement / Look Toggle")]
    [SerializeField] private MonoBehaviour[] movementScripts;
    [SerializeField] private MonoBehaviour[] lookScripts;
    [SerializeField] private Rigidbody fpRigidbody;

    [Header("Focus Notifies")]
    [SerializeField] private KeypadController[] keypadControllers;
    [SerializeField] private ColorPadController[] colorPadControllers;
    [SerializeField] private CrosshairRayInteractor rayInteractor;
    [SerializeField] private RectTransform fpsCrosshair;
    [SerializeField] private Image fpsCrosshairImage;
    [SerializeField] private RectTransform focusCrosshair;
    [SerializeField] private Image focusCrosshairImage;

    [Header("Input")]
    [SerializeField] private InputActionReference exitAction;

    [Header("Cursor")]
    [SerializeField] private bool keepSystemCursorLocked = false;

    [Header("Colliders to Toggle")]
    [SerializeField] private Collider[] collidersToDisableOnFocus;

    private bool focused;

    private void OnEnable()
    {
        exitAction?.action?.Enable();
    }

    private void OnDisable()
    {
        exitAction?.action?.Disable();
        if (focused)
            ExitFocus();
    }

    private void Update()
    {
        if (focused && ExitPressed())
            ExitFocus();
    }

    private void EnterFocus()
    {
        ResolveColorPads();

        focused = true;
        ApplyPriority(fpsVcam, true);
        ApplyPriority(focusVcam, true);
        ToggleArray(movementScripts, false);
        ToggleArray(lookScripts, false);
        if (fpRigidbody != null)
            fpRigidbody.linearVelocity = Vector3.zero;
        NotifyFocus(true);
        ToggleColliders(false);

        if (rayInteractor != null)
        {
            if (focusCrosshair != null)
                rayInteractor.SetCrosshair(focusCrosshair, focusCrosshairImage);
            rayInteractor.SetScreenPointMode(true);
        }

        if (fpsCrosshair != null)
            fpsCrosshair.gameObject.SetActive(false);
        if (focusCrosshair != null)
            focusCrosshair.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
    }

    private void ExitFocus()
    {
        ResolveColorPads();

        focused = false;
        ApplyPriority(fpsVcam, false);
        ApplyPriority(focusVcam, false);
        ToggleArray(movementScripts, true);
        ToggleArray(lookScripts, true);
        NotifyFocus(false);
        ToggleColliders(true);

        if (rayInteractor != null)
        {
            if (fpsCrosshair != null)
                rayInteractor.SetCrosshair(fpsCrosshair, fpsCrosshairImage);
            rayInteractor.SetScreenPointMode(false);
        }

        if (fpsCrosshair != null)
            fpsCrosshair.gameObject.SetActive(true);
        if (focusCrosshair != null)
            focusCrosshair.gameObject.SetActive(false);

        Cursor.lockState = keepSystemCursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !keepSystemCursorLocked;
    }

    private bool ExitPressed()
    {
        if (exitAction != null && exitAction.action != null && exitAction.action.triggered)
            return true;
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            return true;
        var gp = Gamepad.current;
        if (gp != null && gp.buttonEast.wasPressedThisFrame)
            return true;
        return false;
    }

    private void ApplyPriority(VCamPriority config, bool focusedState)
    {
        int target = focusedState ? config.priorityWhenFocused : config.priorityDefault;
        SetPriorityValue(config.vcam, target);
    }

    private void SetPriorityValue(Component cam, int priority)
    {
        if (cam == null)
            return;

        Component target = FindPriorityComponent(cam);
        if (target == null)
        {
            Debug.LogWarning($"VCamFocusInteract: Priority alanı bulunamadı ({cam.name})");
            return;
        }

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        var type = target.GetType();

        var prop = type.GetProperty("Priority", flags);
        if (prop != null && prop.CanWrite)
        {
            var pType = prop.PropertyType;
            if (pType == typeof(int))
            {
                prop.SetValue(target, priority, null);
                return;
            }
            object val = prop.GetValue(target, null);
            if (TrySetValueMember(val, pType, priority))
            {
                prop.SetValue(target, val, null);
                return;
            }
            var ctor = pType.GetConstructor(new[] { typeof(int) });
            if (ctor != null)
            {
                object boxed = ctor.Invoke(new object[] { priority });
                prop.SetValue(target, boxed, null);
                return;
            }
        }

        var field = type.GetField("Priority", flags);
        if (field != null)
        {
            var fType = field.FieldType;
            if (fType == typeof(int))
            {
                field.SetValue(target, priority);
                return;
            }
            object val = field.GetValue(target);
            if (TrySetValueMember(val, fType, priority))
            {
                field.SetValue(target, val);
                return;
            }
            var ctor = fType.GetConstructor(new[] { typeof(int) });
            if (ctor != null)
            {
                object boxed = ctor.Invoke(new object[] { priority });
                field.SetValue(target, boxed);
                return;
            }
        }

        Debug.LogWarning($"VCamFocusInteract: Priority alanı atanamadı ({type.Name})");
    }

    private bool TrySetValueMember(object obj, System.Type type, int priority)
    {
        if (obj == null) return false;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        var valProp = type.GetProperty("Value", flags);
        if (valProp != null && valProp.CanWrite && valProp.PropertyType == typeof(int))
        {
            valProp.SetValue(obj, priority, null);
            return true;
        }
        var valField = type.GetField("Value", flags);
        if (valField != null && valField.FieldType == typeof(int))
        {
            valField.SetValue(obj, priority);
            return true;
        }
        return false;
    }

    private Component FindPriorityComponent(Component cam)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        var t = cam.GetType();
        if (t.GetProperty("Priority", flags) != null || t.GetField("Priority", flags) != null)
            return cam;

        var comps = cam.gameObject.GetComponents<Component>();
        for (int i = 0; i < comps.Length; i++)
        {
            var ct = comps[i].GetType();
            if (ct.GetProperty("Priority", flags) != null || ct.GetField("Priority", flags) != null)
                return comps[i];
        }
        return null;
    }

    private void ToggleArray(MonoBehaviour[] arr, bool state)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] != null)
                arr[i].enabled = state;
    }

    private void ToggleColliders(bool enable)
    {
        if (collidersToDisableOnFocus == null) return;
        for (int i = 0; i < collidersToDisableOnFocus.Length; i++)
        {
            if (collidersToDisableOnFocus[i] != null)
                collidersToDisableOnFocus[i].enabled = enable;
        }
    }

    private void NotifyFocus(bool on)
    {
        ResolveColorPads();

        if (keypadControllers != null)
        {
            for (int i = 0; i < keypadControllers.Length; i++)
                keypadControllers[i]?.SetFocus(on);
        }
        if (colorPadControllers != null)
        {
            for (int i = 0; i < colorPadControllers.Length; i++)
                colorPadControllers[i]?.SetFocus(on);
        }
    }

    private void ResolveColorPads()
    {
        bool missing = colorPadControllers == null || colorPadControllers.Length == 0;
        if (!missing)
        {
            for (int i = 0; i < colorPadControllers.Length; i++)
            {
                if (colorPadControllers[i] == null)
                {
                    missing = true;
                    break;
                }
            }
        }

        if (missing)
        {
            colorPadControllers = FindObjectsOfType<ColorPadController>(true);
        }
    }

    // IInteractable
    public bool CanInteract(GameObject interactor)
    {
        return !focused;
    }

    public void Interact(GameObject interactor)
    {
        if (!focused)
            EnterFocus();
    }

    public void Highlight(bool on, GameObject interactor)
    {
        // özel highlight yok
    }
}
