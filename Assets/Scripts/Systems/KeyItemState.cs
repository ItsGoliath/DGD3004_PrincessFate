using UnityEngine;

/// <summary>
/// Basit anahtar/nesne durumu. 3D item alýndýðýnda true yapýlýr, sahneler arasý taþýnýr.
/// </summary>
public class KeyItemState : MonoBehaviour
{
    public static KeyItemState Instance { get; private set; }

    // Persist etmek için statik yedek; Instance ölse bile deðer saklanýr.
    private static bool savedHasKeyItem;

    [SerializeField] private bool dontDestroyOnLoad = true;

    public bool HasKeyItem { get; private set; }

    /// <summary>
    /// Sahneye eklenmemiþse otomatik oluþturur ve döndürür.
    /// </summary>
    public static KeyItemState EnsureExists()
    {
        if (Instance == null)
        {
            var go = new GameObject("KeyItemState");
            var state = go.AddComponent<KeyItemState>();
            state.dontDestroyOnLoad = true;
        }
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        HasKeyItem = savedHasKeyItem;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    public void GrantKeyItem()
    {
        HasKeyItem = true;
        savedHasKeyItem = true;
    }

    public void ClearKeyItem()
    {
        HasKeyItem = false;
        savedHasKeyItem = false;
    }
}
