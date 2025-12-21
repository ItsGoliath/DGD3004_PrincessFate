using UnityEngine;

/// <summary>
/// Düşman öldüğünde tek bir kağıt parçası prefabını spawn etmek için basit yardımcı.
/// Ölüm anında Drop() çağır. dropOnDestroy açık ise disable/Destroy anında bir kez daha bırakmayı dener.
/// </summary>
public class PaperDropOnDeath : MonoBehaviour
{
    [SerializeField] private GameObject paperPiecePrefab;
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    [Tooltip("Objenin yok edilmesi/disablesı anında otomatik bırak.")]
    [SerializeField] private bool dropOnDestroy = true;

    private bool dropped;

    public void Drop()
    {
        if (paperPiecePrefab == null)
            return;

        Instantiate(paperPiecePrefab, transform.position + spawnOffset, Quaternion.identity);
        dropped = true;
    }

    private void OnDisable()
    {
        // Eğer sahne unload veya kill sırasında Drop çağrılmadıysa buradan bırak.
        if (dropOnDestroy && !dropped && gameObject.scene.isLoaded && Application.isPlaying)
        {
            Drop();
        }
    }
}
