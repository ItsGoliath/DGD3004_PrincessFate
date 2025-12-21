using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Level 2 exit trigger: NPC dialogue finished + all enemies dead -> opens NextDisk UI.
/// Pauses time (opsiyonel), disables controls/audio as needed. Replay button additive yeniden yükler.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NextDiskPanelTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private NPCDialogueTrigger npcDialogue;
    [SerializeField] private Canvas nextDiskCanvas;
    [SerializeField] private bool openOnce = true;
    [SerializeField] private bool requireAllEnemiesDefeated = true;
    [SerializeField] private bool pauseTime = false; // timeScale default kapalý (ESC sonrasý hareket sorun olmasýn)

    [Header("On Open: disable/hide/mute")]
    [SerializeField] private MonoBehaviour[] disableOnOpen;
    [SerializeField] private GameObject[] hideOnOpen;
    [SerializeField] private AudioSource[] muteOnOpen;

    [Header("Replay Options")]
    [SerializeField] private string replaySceneName = "Level2";  // additive yükleyeceðimiz sahne
    [SerializeField] private bool setReplaySceneActive = true;     // yeni sahneyi aktif yap
    [SerializeField] private bool unloadExistingBeforeReplay = true; // ayný sahne zaten yüklüyse önce unload et

    private bool opened;
    private float prevTimeScale = 1f;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (openOnce && opened)
            return;

        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
            return;

        if (requireAllEnemiesDefeated && !AllEnemiesDead())
            return;

        if (npcDialogue != null && !npcDialogue.HasDialogueCompleted)
            return;

        Open();
    }

    private bool AllEnemiesDead()
    {
        var tracker = EnemyTracker.Instance;
        if (tracker == null)
            return true; // tracker yoksa engelleme
        return tracker.AreAllEnemiesDefeated();
    }

    private void Open()
    {
        if (openOnce && opened)
            return;
        opened = true;

        if (pauseTime)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        ToggleBehaviours(disableOnOpen, false);
        ToggleObjects(hideOnOpen, false);
        MuteAudio(muteOnOpen, true);

        if (nextDiskCanvas != null)
            nextDiskCanvas.gameObject.SetActive(true);
    }

    public void CloseAndRestore()
    {
        if (pauseTime)
            Time.timeScale = prevTimeScale;

        ToggleBehaviours(disableOnOpen, true);
        ToggleObjects(hideOnOpen, true);
        MuteAudio(muteOnOpen, false);

        if (nextDiskCanvas != null)
            nextDiskCanvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// Level2'yi additive yeniden yükler (hub/3D sahnesi açýk kalsýn diye). UI butonuna baðla.
    /// Ayný sahne zaten yüklüyse önce unload eder, sonra yeniden yükler.
    /// </summary>
    public void ReplayLevel()
    {
        CloseAndRestore();

        if (string.IsNullOrEmpty(replaySceneName))
            return;

        Time.timeScale = 1f; // güvenlik

        // Eðer sahne zaten yüklüyse önce unload et, sonra yükle
        Scene existing = SceneManager.GetSceneByName(replaySceneName);
        if (unloadExistingBeforeReplay && existing.IsValid())
        {
            var unloadOp = SceneManager.UnloadSceneAsync(existing);
            unloadOp.completed += _ => LoadReplayScene();
        }
        else
        {
            LoadReplayScene();
        }
    }

    private void LoadReplayScene()
    {
        var loadOp = SceneManager.LoadSceneAsync(replaySceneName, LoadSceneMode.Additive);
        loadOp.completed += _ =>
        {
            if (setReplaySceneActive)
            {
                var loaded = SceneManager.GetSceneByName(replaySceneName);
                if (loaded.IsValid())
                    SceneManager.SetActiveScene(loaded);
            }
        };
    }

    private void ToggleBehaviours(MonoBehaviour[] list, bool state)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] != null)
                list[i].enabled = state;
        }
    }

    private void ToggleObjects(GameObject[] list, bool state)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] != null)
                list[i].SetActive(state);
        }
    }

    private void MuteAudio(AudioSource[] list, bool mute)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] != null)
                list[i].mute = mute;
        }
    }
}
