using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 0/1 tuþlarý ile kod girilen basit panel. Klavye giriþi kapalý; sadece AppendBit (butonlardan) ile yazýlýr.
/// </summary>
public class BinaryCodePanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField inputField;

    [Header("Kod")]
    [SerializeField] private string correctCode = "0101";
    [SerializeField] private int maxLength = 4;
    [SerializeField] private bool autoSubmitOnMaxLength = true;

    [Header("Sahne Yükleme")]
    [SerializeField] private string loadSceneName = "PrincessRoom";
    [SerializeField] private bool useTVManager = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onCorrect;
    [SerializeField] private UnityEvent onWrong;

    public void Open()
    {
        gameObject.SetActive(true);
        ClearInput();
        LockInputField();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void AppendBit(string bit)
    {
        if (inputField == null)
            return;
        if (bit != "0" && bit != "1")
            return;
        if (inputField.text.Length >= maxLength)
            return;

        inputField.text += bit;

        if (autoSubmitOnMaxLength && inputField.text.Length >= maxLength)
            Submit();
    }

    public void Submit()
    {
        if (inputField == null)
            return;

        string entered = inputField.text;
        bool ok = string.Equals(entered, correctCode, System.StringComparison.Ordinal);

        if (ok)
        {
            onCorrect?.Invoke();
            LoadScene();
        }
        else
        {
            onWrong?.Invoke();
            ClearInput();
        }
    }

    public void ClearInput()
    {
        if (inputField != null)
            inputField.text = string.Empty;
    }

    private void LockInputField()
    {
        if (inputField == null)
            return;

        inputField.interactable = false;
        inputField.readOnly = true;
        inputField.DeactivateInputField();
        inputField.selectionStringAnchorPosition = 0;
        inputField.selectionStringFocusPosition = 0;
    }

    private void LoadScene()
    {
        if (string.IsNullOrEmpty(loadSceneName))
            return;

        if (useTVManager && TVGameManager.Instance != null)
        {
            TVGameManager.Instance.ChangeLevel(loadSceneName);
        }
        else
        {
            SceneManager.LoadScene(loadSceneName);
        }
    }
}
