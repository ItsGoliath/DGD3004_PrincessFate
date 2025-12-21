using UnityEngine;

/// <summary>
/// Level sonu tetikleyici: Player tetikleyince BinaryCodePanel'i açar, kontrol/müzik vs. kapatýr.
/// Varsayýlan olarak yalnýzca 2D fiziði durdurur (Physics2D.simulationMode = Script), timeScale'e dokunmaz.
/// Ýstenirse timeScale de durdurulabilir. Açýlýþta belirli AudioSource'larý da durdurabilir (footstep vs.).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BinaryCodePanelTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private BinaryCodePanel panel;
    [SerializeField] private Canvas canvasToEnable; // Panel baþka bir canvas'taysa açmadan önce aktif et
    [SerializeField] private bool openOnce = true;
    [SerializeField] private bool requireAllEnemiesDefeated = false;

    [Header("Kapatýlacak Objeler/Scriptler")]
    [SerializeField] private MonoBehaviour[] disableOnOpen;
    [SerializeField] private GameObject[] hideOnOpen;
    [SerializeField] private AudioSource[] muteAudioOnOpen;
    [SerializeField] private AudioSource[] stopAudioOnOpen; // footstep gibi loop'u tamamen durdur

    [Header("Time / Physics")]
    [SerializeField] private bool pauseTime = false;                 // 3D'yi etkilemesin diye varsayýlan false
    [SerializeField] private bool pause2DPhysics = true;             // Physics2D.simulationMode = Script

    private bool opened;
    private float prevTimeScale = 1f;
    private SimulationMode2D prevSimMode = SimulationMode2D.FixedUpdate;

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

        Open();
    }

    private bool AllEnemiesDead()
    {
        var tracker = EnemyTracker.Instance;
        if (tracker == null)
            return true; // tracker yoksa engelleme
        return tracker.AreAllEnemiesDefeated();
    }

    public void Open()
    {
        if (openOnce && opened)
            return;
        opened = true;

        if (pauseTime)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (pause2DPhysics)
        {
            prevSimMode = Physics2D.simulationMode;
            Physics2D.simulationMode = SimulationMode2D.Script;
        }

        ToggleBehaviours(disableOnOpen, false);
        ToggleObjects(hideOnOpen, false);
        MuteAudio(muteAudioOnOpen, true);
        StopAudio(stopAudioOnOpen);

        if (canvasToEnable != null)
            canvasToEnable.gameObject.SetActive(true);

        if (panel != null)
            panel.Open();
    }

    public void CloseAndRestore()
    {
        if (pauseTime)
            Time.timeScale = prevTimeScale;

        if (pause2DPhysics)
            Physics2D.simulationMode = prevSimMode;

        ToggleBehaviours(disableOnOpen, true);
        ToggleObjects(hideOnOpen, true);
        MuteAudio(muteAudioOnOpen, false);

        if (panel != null)
            panel.Close();
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

    private void StopAudio(AudioSource[] list)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] != null)
                list[i].Stop();
        }
    }
}
