using UnityEngine;

/// <summary>
/// Öldürülen düşmandan düşen kağıt parçası pickup'u.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PaperPiecePickup : MonoBehaviour
{
    [SerializeField] private string pieceId;
    [SerializeField] private AudioSource pickupAudio;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private bool destroyOnPickup = true;

    void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (PaperInventory.Instance != null && !string.IsNullOrEmpty(pieceId))
            PaperInventory.Instance.AddPiece(pieceId);

        PlaySfx();

        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    private void PlaySfx()
    {
        if (pickupAudio != null && pickupClip != null)
            pickupAudio.PlayOneShot(pickupClip);
    }
}
