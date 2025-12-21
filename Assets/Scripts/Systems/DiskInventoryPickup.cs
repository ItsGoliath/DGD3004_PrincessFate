using UnityEngine;

/// <summary>
/// Disk pickup: yalnizca manuel cagrı (E/raycast vb.) ile envantere disk ekler.
/// Disk hangi level'i acacagini da envantere yazar. Opsiyonel olarak KeyItemState verebilir.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DiskInventoryPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private string diskId = "Level2Disk";
    [SerializeField] private string targetLevelName = "Level2";
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private bool grantKeyItemOnPickup = false;
    [SerializeField] private AudioSource pickupAudio;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private bool debugLogs = false;

    private bool picked;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    /// <summary>
    /// Uzaktan etkilesim (raycast/E tusu vb.) icin manuel cagrı.
    /// </summary>
    public void Pickup()
    {
        if (picked)
            return;

        picked = true;

        var inv = DiskInventory.EnsureExists();
        if (inv != null)
        {
            if (debugLogs)
                Debug.Log($"DiskInventoryPickup: Pickup -> ekleniyor disk '{diskId}', level '{targetLevelName}'");
            inv.AddDisk(diskId, targetLevelName);
        }
        else if (debugLogs)
        {
            Debug.LogWarning("DiskInventoryPickup: DiskInventory bulunamadi");
        }

        if (grantKeyItemOnPickup)
        {
            var state = KeyItemState.EnsureExists();
            if (state != null)
                state.GrantKeyItem();
        }

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
        // Basit: görsel yok; ileride glow eklenebilir
    }
}
