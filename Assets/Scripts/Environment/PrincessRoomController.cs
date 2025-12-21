using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.InputSystem;

/// <summary>
/// Prenses odası: Final düşman öldüğünde Timeline cutscene'ini oynatır.
/// </summary>
public class PrincessRoomController : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private EnemyController finalEnemy;
    [SerializeField] private PlayableDirector cutsceneDirector;
    [SerializeField] private GameObject gameplayUI;

    [Header("Ayarlar")]
    [Tooltip("Cutscene başladıktan sonra tekrar tetiklenmesin.")]
    [SerializeField] private bool disablePlayerControls = true;
    [SerializeField] private bool disablePlayerComponent = true;
    [SerializeField] private float startDelay = 2f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference skipCutsceneAction;

    private bool cutsceneStarted;
    private float timer;
    private Player cachedPlayer;
    private bool playerComponentDisabled;

    void OnEnable()
    {
        skipCutsceneAction?.action?.Enable();
    }

    void OnDisable()
    {
        skipCutsceneAction?.action?.Disable();
    }

    void Update()
    {
        if (cutsceneStarted)
        {
            if (cutsceneDirector != null && cutsceneDirector.state == PlayState.Playing)
            {
                if (SkipPressedThisFrame())
                    cutsceneDirector.time = cutsceneDirector.duration;
            }
            return;
        }

        timer += Time.deltaTime;
        if (timer >= startDelay)
            StartCutscene();
    }

    private void StartCutscene()
    {
        cutsceneStarted = true;

        if (disablePlayerControls)
            DisablePlayerControl();

        if (gameplayUI != null)
            gameplayUI.SetActive(false);

        if (cutsceneDirector != null)
        {
            cutsceneDirector.stopped += OnCutsceneStopped;
            cutsceneDirector.Play();
        }
        else
        {
            QuitGame();
        }
    }

    private void OnCutsceneStopped(PlayableDirector director)
    {
        director.stopped -= OnCutsceneStopped;
        RestorePlayerComponent();
        QuitGame();
    }

    private void DisablePlayerControl()
    {
        Player player = FindOrCachePlayer();
        if (player == null)
            return;

        player.SetMovementLock(true);
        if (disablePlayerComponent && player.isActiveAndEnabled)
        {
            player.enabled = false;
            playerComponentDisabled = true;
        }
    }

    private Player FindOrCachePlayer()
    {
        if (cachedPlayer != null)
            return cachedPlayer;

        cachedPlayer = FindObjectOfType<Player>();
        return cachedPlayer;
    }

    private void RestorePlayerComponent()
    {
        if (!playerComponentDisabled || cachedPlayer == null)
            return;

        cachedPlayer.enabled = true;
        playerComponentDisabled = false;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private bool SkipPressedThisFrame()
    {
        if (skipCutsceneAction != null && skipCutsceneAction.action != null)
            return skipCutsceneAction.action.triggered;

        return false;
    }
}
