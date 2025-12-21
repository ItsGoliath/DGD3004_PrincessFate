using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// TMP_InputField'i sadece kod tarafindan degistirilecek hale getirir; klavye ile yazmayi engeller.
/// Input field'a tiklandiginda veya secildiginde focus'u hemen kaldirir.
/// </summary>
[RequireComponent(typeof(TMP_InputField))]
public class InputFieldLock : MonoBehaviour, ISelectHandler, IPointerClickHandler
{
    [SerializeField] private TMP_InputField input;

    private void Reset()
    {
        input = GetComponent<TMP_InputField>();
    }

    private void Awake()
    {
        if (input == null) input = GetComponent<TMP_InputField>();
    }

    private void OnEnable()
    {
        if (input == null) return;

        // Klavye ile yazilmasin, sanal klavye acilmasin
        input.readOnly = true;
        input.shouldHideSoftKeyboard = true;

        // Baslangicta focus'i kaldir
        Deactivate();
    }

    public void OnSelect(BaseEventData eventData)
    {
        Deactivate();
        StartCoroutine(ClearSelectionNextFrame());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Deactivate();
        StartCoroutine(ClearSelectionNextFrame());
    }

    private void Deactivate()
    {
        if (input == null) return;
        input.DeactivateInputField();
    }

    private IEnumerator ClearSelectionNextFrame()
    {
        yield return null; // bir sonraki frame'e kadar bekle
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == input.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
