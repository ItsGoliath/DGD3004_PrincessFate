using UnityEngine;

/// <summary>
/// Basit buton/objeden çağrılır: hedefi aktif eder, kendini (veya belirtilen objeyi) kapatır.
/// </summary>
public class PaperPieceActivator : MonoBehaviour
{
    [SerializeField] private GameObject targetToShow;
    [SerializeField] private GameObject objectToHide; // boşsa bu component'in GameObject'i

    public void Activate()
    {
        if (targetToShow != null)
            targetToShow.SetActive(true);

        GameObject hideObj = objectToHide != null ? objectToHide : gameObject;
        if (hideObj != null)
            hideObj.SetActive(false);
    }
}
