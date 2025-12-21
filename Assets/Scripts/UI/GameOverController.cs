using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Oyuncu öldüğünde veya belirli bir y seviyesinin altına düştüğünde "You Lost" UI'sını açar ve restart/next aksiyonlarını dinler.
/// </summary>
public class GameOverController : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private Player player;
    [SerializeField] private GameObject youLostUI;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Ayarlar")]
    [SerializeField] private float fallThresholdY = -20f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string nextSceneName = "";
    [SerializeField] private string youLostUITag = "YouLostUI";
    [SerializeField] private string youLostUIName = "";

    [Header("Input Actions")]
    [SerializeField] private InputActionReference restartAction;
    [SerializeField] private InputActionReference nextSceneAction;

    private bool gameOverTriggered;

    void Awake()
    {
        if (player == null)
            FindPlayer();

        RefreshYouLostUI();
        SetYouLostUI(false);

        SetupMusic();
    }

    void OnEnable()
    {
        restartAction?.action?.Enable();
        nextSceneAction?.action?.Enable();
    }

    void OnDisable()
    {
        restartAction?.action?.Disable();
        nextSceneAction?.action?.Disable();
    }

    void Update()
    {
        if (nextSceneAction != null && nextSceneAction.action != null && nextSceneAction.action.triggered)
            LoadNextScene();

        if (gameOverTriggered)
        {
            if (restartAction != null && restartAction.action != null && restartAction.action.triggered)
                ReloadScene();
            return;
        }

        if (player == null)
            FindPlayer();

        if (player == null)
            return;

        if (player.IsDead())
        {
            TriggerGameOver();
            return;
        }

        if (player.transform.position.y <= fallThresholdY)
        {
            player.ForceKill();
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        if (gameOverTriggered)
            return;

        gameOverTriggered = true;
        RefreshYouLostUI();
        SetYouLostUI(true);
    }

    private void ReloadScene()
    {
        string sceneName = gameObject.scene.name;
        if (!TryLoadViaTV(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }
    private void SetupMusic()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
            if (musicSource == null && backgroundMusic != null)
                musicSource = gameObject.AddComponent<AudioSource>();
        }

        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            if (!musicSource.isPlaying)
                musicSource.Play();
        }
    }

    private void FindPlayer()
    {
        if (!string.IsNullOrEmpty(playerTag))
        {
            GameObject found = GameObject.FindGameObjectWithTag(playerTag);
            if (found != null)
                player = found.GetComponent<Player>();
        }
    }

    private void RefreshYouLostUI()
    {
        if (youLostUI != null)
        {
            if (youLostUI.scene.IsValid() && youLostUI.scene.isLoaded)
                return;
        }
        youLostUI = null;

        if (!string.IsNullOrEmpty(youLostUITag))
        {
            GameObject found = GameObject.FindWithTag(youLostUITag);
            if (found != null)
                youLostUI = found;
        }

        if (youLostUI == null && !string.IsNullOrEmpty(youLostUIName))
        {
            GameObject foundByName = GameObject.Find(youLostUIName);
            if (foundByName != null)
                youLostUI = foundByName;
        }
    }

    private void SetYouLostUI(bool state)
    {
        if (youLostUI == null)
            RefreshYouLostUI();

        if (youLostUI != null)
            youLostUI.SetActive(state);
    }

    private void LoadNextScene()
    {
        string target = nextSceneName;

        if (string.IsNullOrEmpty(target))
        {
            Scene current = gameObject.scene;
            int currentIndex = current.buildIndex;
            int nextIndex = currentIndex + 1;
            if (nextIndex >= SceneManager.sceneCountInBuildSettings)
                nextIndex = currentIndex;

            target = SceneManager.GetSceneByBuildIndex(nextIndex).name;
        }

        if (!TryLoadViaTV(target))
        {
            SceneManager.LoadScene(target);
        }
    }

    private bool TryLoadViaTV(string sceneName)
    {
        if (TVGameManager.Instance == null || string.IsNullOrEmpty(sceneName))
            return false;

        TVGameManager.Instance.ChangeLevel(sceneName);
        return true;
    }
}
