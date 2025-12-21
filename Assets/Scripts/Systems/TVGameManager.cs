using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// 3D oda sahnesini açýk tutup 2D sahneleri additive yükleyerek kameralarýný TV'ye yönlendirir.
/// </summary>
public class TVGameManager : MonoBehaviour
{
    public static TVGameManager Instance { get; private set; }
    [Header("TV Baðlantý Ayarlarý")]
    [SerializeField] private RenderTexture tvRenderTexture;
    [SerializeField] private string firstSceneName = "Level1";
    [SerializeField] private string cameraTag = "2DGameCamera";
    [SerializeField] private LayerMask cullingMask = ~0;
    [SerializeField] private bool setOrthographic = true;
    [SerializeField] private bool disable2DAudioListener = true;
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField] private bool loadOnStart = false;
    [SerializeField] private bool confineCursorOnLoad = true;
    [SerializeField] private bool hideSystemCursorInRoom = true;
    [SerializeField] private bool manageCursor = true;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference loadHotkeyAction;
    [Header("Fallback Hotkey")]
    [SerializeField] private bool useKeyFallback = true;
    [SerializeField] private KeyCode loadHotkey = KeyCode.Y;

    private Camera current2DCamera;
    private bool isLoading;
    private bool firstSceneLoaded;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        loadHotkeyAction?.action?.Enable();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        loadHotkeyAction?.action?.Disable();
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        if (hideSystemCursorInRoom && manageCursor)
            HideSystemCursor();

        if (loadOnStart && !string.IsNullOrEmpty(firstSceneName))
            StartCoroutine(Load2DSceneAdditive(firstSceneName));
    }

    void Update()
    {
        if (isLoading || firstSceneLoaded)
            return;

        if (LoadPressedThisFrame() && !string.IsNullOrEmpty(firstSceneName))
            StartCoroutine(Load2DSceneAdditive(firstSceneName));
    }

    public void ChangeLevel(string newSceneName)
    {
        if (string.IsNullOrEmpty(newSceneName))
            return;

        StartCoroutine(Load2DSceneAdditive(newSceneName));
    }

    private IEnumerator Load2DSceneAdditive(string sceneName)
    {
        if (isLoading)
            yield break;

        isLoading = true;

        if (current2DCamera != null)
        {
            Scene oldScene = current2DCamera.gameObject.scene;
            if (oldScene.IsValid() && oldScene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(oldScene);

            current2DCamera = null;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (op != null && !op.isDone)
            yield return null;

        isLoading = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Additive)
            return;

        Camera cam = Find2DCameraInScene(scene);
        if (cam == null)
            return;

        Setup2DCamera(cam);
        current2DCamera = cam;
        firstSceneLoaded = true;

        if (confineCursorOnLoad && manageCursor)
            HideSystemCursor();
    }

    private Camera Find2DCameraInScene(Scene scene)
    {
        Camera[] cameras = GameObject.FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (!string.IsNullOrEmpty(cameraTag) && !cam.CompareTag(cameraTag))
                continue;

            if (cam.gameObject.scene == scene)
                return cam;
        }

        return null;
    }

    private void Setup2DCamera(Camera cam)
    {
        if (setOrthographic)
            cam.orthographic = true;

        if (tvRenderTexture != null)
            cam.targetTexture = tvRenderTexture;

        if (cullingMask.value != 0)
            cam.cullingMask = cullingMask;

        if (disable2DAudioListener)
        {
            AudioListener listener = cam.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = false;
        }
    }

    private void HideSystemCursor()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }

    private bool LoadPressedThisFrame()
    {
        if (loadHotkeyAction != null && loadHotkeyAction.action != null)
            return loadHotkeyAction.action.triggered;

        if (useKeyFallback && loadHotkey != KeyCode.None)
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb[ConvertKey(loadHotkey)].wasPressedThisFrame)
                return true;
        }

        return false;
    }

    private Key ConvertKey(KeyCode keyCode)
    {
        // Basic mapping for letters/numbers; extend if needed
        if (System.Enum.TryParse<Key>(keyCode.ToString(), out var key))
            return key;
        return Key.None;
    }
}
