using UnityEngine;

/// <summary>
/// Inspector'dan FPS s\u0131n\u0131r\u0131 veya VSync ayar\u0131 verir; build'de de \u00e7al\u0131\u015f\u0131r.
/// targetFrameRate = 0 -> s\u0131n\u0131rs\u0131z (Unity varsay\u0131lan\u0131). VSync a\u00e7\u0131ksa targetFrameRate devre d\u0131\u015f\u0131 kal\u0131r.
/// </summary>
public class FPSLimiter : MonoBehaviour
{
    [Header("FPS / VSync")]
    [Tooltip("Hedef FPS. 0 = s\u0131n\u0131rs\u0131z (vSync kapaliyken ge\u00e7erli)")]
    [SerializeField] private int targetFrameRate = 60;
    [Tooltip("VSync a\u00e7\u0131ls\u0131n m\u0131? A\u00e7\u0131ksa targetFrameRate yok say\u0131l\u0131r")]
    [SerializeField] private bool useVSync = false;
    [Tooltip("VSync count (1 = her kare, 2 = her iki kare)")]
    [SerializeField] private int vSyncCount = 1;
    [Tooltip("Ba\u015flang\u0131\u00e7ta otomatik uygula")]
    [SerializeField] private bool applyOnStart = true;

    private void Start()
    {
        if (applyOnStart)
            Apply();
    }

    /// <summary>Inspector butonuna ba\u011flanabilir.</summary>
    public void Apply()
    {
        if (useVSync)
        {
            QualitySettings.vSyncCount = Mathf.Max(0, vSyncCount);
            Application.targetFrameRate = -1; // Unity default, vSync belirleyici
            Debug.Log($"FPSLimiter: vSync enabled (count={QualitySettings.vSyncCount}), targetFrameRate ignored");
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
            Debug.Log($"FPSLimiter: targetFrameRate set to {targetFrameRate}, vSync=0");
        }
    }
}
