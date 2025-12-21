using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Görünmez duvara temas edildiğinde sahneyi değiştirir; tüm düşmanlar ölmeden tetiklenmez.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelExitTrigger : MonoBehaviour
{
    [SerializeField] private string requiredTag = "Player";
    [SerializeField] private string nextSceneName = "Level2";
    [SerializeField] private bool requireAllEnemiesDefeated = true;
    [SerializeField] private bool disableAfterUse = true;

    private bool triggered;

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered)
            return;

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        if (requireAllEnemiesDefeated && EnemyTracker.Instance != null && !EnemyTracker.Instance.AreAllEnemiesDefeated())
            return;

        triggered = true;

        if (TVGameManager.Instance != null)
        {
            TVGameManager.Instance.ChangeLevel(nextSceneName);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }

        if (disableAfterUse)
            gameObject.SetActive(false);
    }
}
