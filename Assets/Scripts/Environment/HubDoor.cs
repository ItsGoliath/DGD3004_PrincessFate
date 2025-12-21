using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Hub kapýsýna girince etkileþim aksiyonu ile istenen sahneyi yükler.
/// TVGameManager varsa ChangeLevel çaðýrýr; yoksa varsayýlan additive yükler (hub açýk kalsýn diye).
/// </summary>
public class HubDoor : MonoBehaviour
{
    [Tooltip("Bu kapýnýn açacaðý sahnenin adý (Build Settings'te ekli olmalý).")]
    public string sceneToLoad;

    [Tooltip("Kapýnýn üstünde gösterilecek 'E' UI objesi.")]
    public GameObject promptUI;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference interactAction;

    [Tooltip("Sadece bu tag'e sahip objelerle etkileþime izin ver.")]
    [SerializeField] private bool requirePlayerTag = true;
    [SerializeField] private string playerTag = "Player"; // 2D oyunda Player tag'i kullanýlýr

    [Header("Yükleme Seçenekleri")]
    [SerializeField] private bool loadAdditive = true;           // Hub açýk kalsýn
    [SerializeField] private bool setLoadedAsActive = true;      // Yeni sahneyi aktif yap
    [SerializeField] private bool unloadCurrentAfterLoad = false;// Gerekirse hub'ý kapat

    private bool playerInside;

    void Start()
    {
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    void OnEnable()
    {
        interactAction?.action?.Enable();
    }

    void OnDisable()
    {
        interactAction?.action?.Disable();
    }

    void Update()
    {
        if (!playerInside)
            return;

        if (InteractPressedThisFrame() && !string.IsNullOrEmpty(sceneToLoad))
        {
            if (TVGameManager.Instance != null)
            {
                TVGameManager.Instance.ChangeLevel(sceneToLoad);
            }
            else
            {
                var mode = loadAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single;
                var op = SceneManager.LoadSceneAsync(sceneToLoad, mode);

                if (loadAdditive)
                {
                    op.completed += _ =>
                    {
                        if (setLoadedAsActive)
                        {
                            var sc = SceneManager.GetSceneByName(sceneToLoad);
                            if (sc.IsValid())
                                SceneManager.SetActiveScene(sc);
                        }

                        if (unloadCurrentAfterLoad)
                            SceneManager.UnloadSceneAsync(gameObject.scene);
                    };
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValidCollider(other))
            return;

        playerInside = true;
        if (promptUI != null)
            promptUI.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsValidCollider(other))
            return;

        playerInside = false;
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    private bool IsValidCollider(Collider2D other)
    {
        if (!requirePlayerTag)
            return true;

        if (string.IsNullOrEmpty(playerTag))
            return true;

        return other.CompareTag(playerTag);
    }

    private bool InteractPressedThisFrame()
    {
        if (interactAction != null && interactAction.action != null)
            return interactAction.action.triggered;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
            return true;

        Gamepad gp = Gamepad.current;
        if (gp != null && gp.buttonWest.wasPressedThisFrame)
            return true;

        return false;
    }
}
