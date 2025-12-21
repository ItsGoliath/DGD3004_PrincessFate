using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class FirstPersonMovement : MonoBehaviour
{
    public float speed = 5f;

    [Header("Running")]
    public bool canRun = true;
    public bool IsRunning { get; private set; }
    public float runSpeed = 9f;
    public KeyCode runningKey = KeyCode.LeftShift;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;   // Vector2
    [SerializeField] private InputActionReference runAction;    // Button/axis

    private Rigidbody rigidbody;
    /// <summary>Functions to override movement speed. Will use the last added override.</summary>
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        moveAction?.action?.Enable();
        runAction?.action?.Enable();
    }

    void OnDisable()
    {
        moveAction?.action?.Disable();
        runAction?.action?.Disable();
    }

    void FixedUpdate()
    {
        IsRunning = canRun && GetRunPressed();

        float targetMovingSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0)
        {
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        Vector2 move = ReadMoveInput();
        Vector2 targetVelocity = move * targetMovingSpeed;

        rigidbody.linearVelocity = transform.rotation * new Vector3(targetVelocity.x, rigidbody.linearVelocity.y, targetVelocity.y);
    }

    private Vector2 ReadMoveInput()
    {
        if (moveAction != null && moveAction.action != null)
            return Vector2.ClampMagnitude(moveAction.action.ReadValue<Vector2>(), 1f);

        Vector2 move = Vector2.zero;
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                move.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                move.x += 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
                move.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                move.y -= 1f;
        }

        Gamepad gp = Gamepad.current;
        if (gp != null)
            move += gp.leftStick.ReadValue();

        return Vector2.ClampMagnitude(move, 1f);
    }

    private bool GetRunPressed()
    {
        if (runAction != null && runAction.action != null)
        {
            float val = runAction.action.ReadValue<float>();
            if (runAction.action.activeControl != null && runAction.action.activeControl is ButtonControl button)
                return button.isPressed;
            return val > 0.5f;
        }

        Keyboard kb = Keyboard.current;
        if (kb != null && runningKey != KeyCode.None)
        {
            Key key;
            if (System.Enum.TryParse(runningKey.ToString(), out key))
            {
                var ctrl = kb[key];
                if (ctrl != null && ctrl.isPressed)
                    return true;
            }
        }

        Gamepad gp = Gamepad.current;
        if (gp != null)
            return gp.leftStickButton.isPressed || gp.buttonWest.isPressed;

        return false;
    }
}
