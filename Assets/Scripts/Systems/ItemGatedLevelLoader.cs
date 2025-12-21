using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Bilgisayar / keypad etkileşimi: E (veya action) ile çalışır, son alınan disk varsa onun level'ini yükler.
/// </summary>
public class ItemGatedLevelLoader : MonoBehaviour, IInteractable
{
    [Header("Input")]
    [SerializeField] private InputActionReference interactAction;

    [Header("Tag (3D Player)")]
    [SerializeField] private string playerTag = "Player3D";

    [Header("Key Gerekli mi?")]
    [SerializeField] private bool requireKeyItem = false;
    [SerializeField] private bool consumeKeyOnSuccess = true;

    [Header("Disk Ayarları")]
    [SerializeField] private bool consumeDiskOnSuccess = true;
    [SerializeField] private bool allowMultipleLoads = true;

    [Header("Disable After Success")]
    [SerializeField] private bool disableAfterSuccess = true;
    [SerializeField] private Collider[] collidersToDisable;
    [SerializeField] private GameObject[] objectsToDisable;

    [Header("Feedback")]
    [SerializeField] private UnityEvent onLoadSuccess;
    [SerializeField] private UnityEvent onLoadFailed; // item yoksa

    private bool playerInside;
    private bool alreadyLoaded;

    private void OnEnable()
    {
        interactAction?.action?.Enable();
    }

    private void OnDisable()
    {
        interactAction?.action?.Disable();
    }

    private void Update()
    {
        if (!playerInside)
            return;

        if (!InteractPressedThisFrame())
            return;

        TryLoad();
    }

    /// <summary>
    /// Raycast veya başka bir yerden çağrıldığında yüklemeyi dener.
    /// </summary>
    public bool TryLoad()
    {
        if (!allowMultipleLoads && alreadyLoaded)
            return false;

        if (!HasRequiredItems(false))
        {
            onLoadFailed?.Invoke();
            return false;
        }

        alreadyLoaded = true;
        onLoadSuccess?.Invoke();

        string levelToLoad;
        string diskId;
        DiskInventory inv = DiskInventory.EnsureExists();
        inv.TryGetLastDisk(out diskId, out levelToLoad);

        if (consumeDiskOnSuccess && !string.IsNullOrEmpty(diskId))
        {
            inv.RemoveDisk(diskId);
        }

        if (consumeKeyOnSuccess)
        {
            var st = KeyItemState.EnsureExists();
            if (st != null)
                st.ClearKeyItem();
        }

        if (disableAfterSuccess)
        {
            if (collidersToDisable != null)
            {
                for (int i = 0; i < collidersToDisable.Length; i++)
                {
                    if (collidersToDisable[i] != null)
                        collidersToDisable[i].enabled = false;
                }
            }
            if (objectsToDisable != null)
            {
                for (int i = 0; i < objectsToDisable.Length; i++)
                {
                    if (objectsToDisable[i] != null)
                        objectsToDisable[i].SetActive(false);
                }
            }
        }

        Debug.Log($"ItemGatedLevelLoader: Yükleniyor -> {levelToLoad} (disk: {diskId})");

        if (TVGameManager.Instance != null)
            TVGameManager.Instance.ChangeLevel(levelToLoad);
        else
            SceneManager.LoadScene(levelToLoad);

        if (allowMultipleLoads)
            alreadyLoaded = false;

        return true;
    }

    private bool HasRequiredItems(bool logFail)
    {
        var state = KeyItemState.EnsureExists();
        if (requireKeyItem && (state == null || !state.HasKeyItem))
        {
            if (logFail) Debug.LogWarning("ItemGatedLevelLoader: KeyItem yok, yükleme iptal.");
            return false;
        }

        string levelToLoad;
        string diskId;
        var inv = DiskInventory.EnsureExists();
        if (inv == null || !inv.TryGetLastDisk(out diskId, out levelToLoad) || string.IsNullOrEmpty(levelToLoad))
        {
            if (logFail) Debug.LogWarning("ItemGatedLevelLoader: Envanterde kullanılabilir disk yok ya da level boş.");
            return false;
        }
        return true;
    }

    private bool InteractPressedThisFrame()
    {
        if (interactAction != null && interactAction.action != null && interactAction.action.triggered)
            return true;

        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
            return true;

        var gp = Gamepad.current;
        if (gp != null && gp.buttonWest.wasPressedThisFrame)
            return true;

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
            playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
            playerInside = false;
    }

    // IInteractable
    public bool CanInteract(GameObject interactor)
    {
        if (!allowMultipleLoads && alreadyLoaded)
            return false;
        return HasRequiredItems(false);
    }

    public void Interact(GameObject interactor)
    {
        TryLoad();
    }

    public void Highlight(bool on, GameObject interactor)
    {
        // isteğe bağlı görsel
    }
}
