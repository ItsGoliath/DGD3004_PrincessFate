using System.Collections;
using UnityEngine;

/// <summary>
/// 3D keypad tuþu: KeypadController'a basýldýðýnda deðer ekler, tuþu aþaðý-yukarý animasyonlar.
/// </summary>
[RequireComponent(typeof(Collider))]
public class KeypadButton : MonoBehaviour, IInteractable
{
    [Tooltip("Tuþ deðeri. 0-9 sayýlar veya Clear/Enter.")]
    public string keyValue = "1";

    [Header("Basma Animasyonu")]
    [SerializeField] private Vector3 pressLocalOffset = new Vector3(0f, -0.06f, 0f);
    [SerializeField] private float pressDownTime = 0.05f;
    [SerializeField] private float returnTime = 0.08f;

    private Vector3 initialLocalPos;
    private Coroutine pressRoutine;

    private void Awake()
    {
        initialLocalPos = transform.localPosition;
    }

    private void OnEnable()
    {
        transform.localPosition = initialLocalPos;
    }

    public void Press(KeypadController controller)
    {
        if (controller == null)
            return;

        if (pressRoutine != null)
            StopCoroutine(pressRoutine);
        pressRoutine = StartCoroutine(AnimatePress());

        string lower = keyValue.ToLowerInvariant();
        if (lower == "clear")
        {
            controller.ClearInput();
            return;
        }

        if (lower == "enter")
        {
            controller.SubmitCode();
            return;
        }

        controller.AppendDigit(keyValue);
    }

    private IEnumerator AnimatePress()
    {
        Vector3 targetDown = initialLocalPos + pressLocalOffset;
        float t = 0f;
        while (t < pressDownTime)
        {
            t += Time.deltaTime;
            float alpha = pressDownTime > 0f ? Mathf.Clamp01(t / pressDownTime) : 1f;
            transform.localPosition = Vector3.Lerp(initialLocalPos, targetDown, alpha);
            yield return null;
        }

        t = 0f;
        while (t < returnTime)
        {
            t += Time.deltaTime;
            float alpha = returnTime > 0f ? Mathf.Clamp01(t / returnTime) : 1f;
            transform.localPosition = Vector3.Lerp(targetDown, initialLocalPos, alpha);
            yield return null;
        }

        transform.localPosition = initialLocalPos;
        pressRoutine = null;
    }

    // IInteractable
    public bool CanInteract(GameObject interactor)
    {
        var controller = GetComponentInParent<KeypadController>();
        if (controller == null)
            return false;
        if (controller.IsColorSolved)
            return false;
        if (controller.RequireFocus && !controller.HasFocus)
            return false;
        return true;
    }

    public void Interact(GameObject interactor)
    {
        var controller = GetComponentInParent<KeypadController>();
        Press(controller);
    }

    public void Highlight(bool on, GameObject interactor)
    {
        // özel highlight yok
    }
}
