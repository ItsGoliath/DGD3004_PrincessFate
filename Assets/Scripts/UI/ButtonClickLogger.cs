using UnityEngine;

/// <summary>
/// UI buton OnClick'ine bağlayıp basıldığında konsola log yazar.
/// </summary>
public class ButtonClickLogger : MonoBehaviour
{
    [SerializeField] private string message = "Button clicked";

    public void LogClick()
    {
        Debug.Log(message);
    }
}
