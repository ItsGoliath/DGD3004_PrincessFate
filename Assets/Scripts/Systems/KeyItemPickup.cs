using UnityEngine;

/// <summary>
/// 3D item pickup: Player tetikleyince veya etkileşimle çağrılınca KeyItemState'e anahtar verir.
/// </summary>
[RequireComponent(typeof(Collider))]
public class KeyItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private AudioSource pickupAudio;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private string requiredTag = "Player3D";

    private bool picked;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        Pickup();
    }

    /// <summary>
    /// Uzaktan etkileşim (raycast/E tuşu vb.) için manuel çağrı.
    /// </summary>
    public void Pickup()
    {
        if (picked)
            return;

        picked = true;

        var state = KeyItemState.EnsureExists();
        if (state != null)
            state.GrantKeyItem();

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

    // IInteractable
    public bool CanInteract(GameObject interactor)
    {
        return !picked;
    }

    public void Interact(GameObject interactor)
    {
        Pickup();
    }

    public void Highlight(bool on, GameObject interactor)
    {
        // isteğe bağlı görsel
    }
}
