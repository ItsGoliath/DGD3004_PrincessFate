using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string hubSceneName = "Hub";
    [SerializeField] private bool loadAdditive = true;          // 3D oda kalsýn diye additive yükle
    [SerializeField] private bool unloadMenuAfterLoad = true;   // Hub yüklenince menü sahnesini kapat
    [SerializeField] private bool setLoadedAsActive = true;     // Hub sahnesini aktif yap

    public void PlayGame()
    {
        if (string.IsNullOrEmpty(hubSceneName))
            return;

        var mode = loadAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single;
        var op = SceneManager.LoadSceneAsync(hubSceneName, mode);

        if (loadAdditive)
        {
            op.completed += _ =>
            {
                if (setLoadedAsActive)
                {
                    var hub = SceneManager.GetSceneByName(hubSceneName);
                    if (hub.IsValid())
                        SceneManager.SetActiveScene(hub);
                }

                if (unloadMenuAfterLoad)
                    SceneManager.UnloadSceneAsync(gameObject.scene);
            };
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
