using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Puzzle alanına girince UI açar. Varsayılan olarak dışarı çıkınca kapanmaz; şartları ve kontrol kapatma seçenekleri içerir.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PaperPuzzleStation : MonoBehaviour
{
    [SerializeField] private GameObject puzzleUI;
    [SerializeField] private UnityEvent onEnter;
    [SerializeField] private UnityEvent onExit;

    [Header("Şartlar")]
    [SerializeField] private string requiredTag = "Player";
    [SerializeField] private bool requireAllEnemiesDefeated = true;

    [Header("Kapanış/Açılış Seçenekleri")]
    [SerializeField] private bool autoCloseOnExit = false;
    [SerializeField] private bool openOnlyOnce = true;

    [Header("Puzzle açılınca kapatılacaklar")]
    [SerializeField] private GameObject[] deactivateObjects;
    [SerializeField] private MonoBehaviour[] deactivateBehaviours;
    [SerializeField] private AudioSource[] pauseAudioSources;
    [SerializeField] private bool pauseTimeScale = true;
    [SerializeField] private bool ignoreTriggerColliders = true;

    private bool puzzleOpen;
    private bool openedOnce;
    private float prevTimeScale = 1f;
    private int overlapCount;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (ignoreTriggerColliders && other.isTrigger)
            return;

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        if (requireAllEnemiesDefeated && EnemyTracker.Instance != null && !EnemyTracker.Instance.AreAllEnemiesDefeated())
            return;

        overlapCount++;
        if (overlapCount == 1)
            OpenPuzzle();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!autoCloseOnExit)
            return;

        if (ignoreTriggerColliders && other.isTrigger)
            return;

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        overlapCount = Mathf.Max(0, overlapCount - 1);
        if (overlapCount == 0)
            ClosePuzzle();
    }

    public void OpenPuzzle()
    {
        if (puzzleOpen)
            return;

        if (openOnlyOnce && openedOnce)
            return;

        openedOnce = true;
        puzzleOpen = true;
        onEnter?.Invoke();

        if (pauseTimeScale)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        ToggleObjects(false);
        ToggleBehaviours(false);
        PauseAudio(true);

        if (puzzleUI != null)
            puzzleUI.SetActive(true);
    }

    public void ClosePuzzle()
    {
        if (!puzzleOpen)
            return;

        puzzleOpen = false;
        onExit?.Invoke();

        if (pauseTimeScale)
            Time.timeScale = prevTimeScale;

        ToggleObjects(true);
        ToggleBehaviours(true);
        PauseAudio(false);

        if (puzzleUI != null)
            puzzleUI.SetActive(false);
    }

    private void ToggleObjects(bool state)
    {
        if (deactivateObjects == null)
            return;

        for (int i = 0; i < deactivateObjects.Length; i++)
        {
            if (deactivateObjects[i] != null)
                deactivateObjects[i].SetActive(state);
        }
    }

    private void ToggleBehaviours(bool state)
    {
        if (deactivateBehaviours == null)
            return;

        for (int i = 0; i < deactivateBehaviours.Length; i++)
        {
            if (deactivateBehaviours[i] != null)
                deactivateBehaviours[i].enabled = state;
        }
    }

    private void PauseAudio(bool pause)
    {
        if (pauseAudioSources == null)
            return;

        for (int i = 0; i < pauseAudioSources.Length; i++)
        {
            AudioSource src = pauseAudioSources[i];
            if (src == null)
                continue;

            if (pause)
                src.Pause();
            else
                src.UnPause();
        }
    }

    private void OnDisable()
    {
        if (puzzleOpen)
            ClosePuzzle();
        overlapCount = 0;
    }
}
