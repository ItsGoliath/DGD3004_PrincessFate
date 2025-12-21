using System.Collections;
using UnityEngine;

/// <summary>
/// Keypad doðru kod girildiðinde disk animasyonunu oynatýr ve animasyon sonunda pickup'u etkinleþtirir.
/// </summary>
public class DiskRevealController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string revealTrigger = "Reveal";
    [SerializeField] private GameObject diskPickup; // DiskInventoryPickup taþýyan obje
    [SerializeField] private float enableDelay = 0f; // animasyon süresi kadar beklemek istersen

    private bool played;

    public void PlayReveal()
    {
        if (played)
            return;
        played = true;

        bool triggered = false;
        if (animator != null && !string.IsNullOrEmpty(revealTrigger))
        {
            animator.SetTrigger(revealTrigger);
            triggered = true;
        }

        if (enableDelay > 0f)
        {
            StartCoroutine(EnableAfterDelay(enableDelay));
        }
        else if (!triggered)
        {
            // Animatör yoksa veya trigger kullanýlmýyorsa direkt aç
            EnableDisk();
        }
        // Eðer animasyon kullanýyorsan, OnRevealComplete event'iyle EnableDisk çaðrýlmalý.
    }

    // Animasyon eventinden de çaðrýlabilir
    public void OnRevealComplete()
    {
        EnableDisk();
    }

    private IEnumerator EnableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EnableDisk();
    }

    private void EnableDisk()
    {
        if (diskPickup != null)
            diskPickup.SetActive(true);
    }
}
