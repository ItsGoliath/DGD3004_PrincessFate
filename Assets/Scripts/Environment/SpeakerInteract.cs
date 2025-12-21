using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hoparlör: Oyuncu E'ye bastýðýnda morse benzeri kýsa/uzun beep dizisini çalar.
/// Kod bittiðinde susar; çalarken tekrar baþlatýlmaz.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SpeakerInteract : MonoBehaviour, IInteractable
{
    [SerializeField] private AudioSource audioSource;
    [Header("Clips")]
    [SerializeField] private AudioClip shortClip; // 0
    [SerializeField] private AudioClip longClip;  // 1
    [Header("Kod (0/1 dizisi)")]
    [SerializeField] private string code = "0101";

    [Header("Süreler")]
    [SerializeField] private float shortDuration = 0.2f;
    [SerializeField] private float longDuration = 0.6f;
    [SerializeField] private float gapDuration = 0.2f;

    [Header("Tag (3D Player)")]
    [SerializeField] private string playerTag = "Player3D";
    [SerializeField] private bool requireTrigger = false;

    private bool playerInside;
    private bool isPlaying;
    private Coroutine playRoutine;
    private int playToken;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    public void PlayFromRay()
    {
        if (isPlaying)
            return;

        if (playRoutine != null)
            StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(PlayCode());
    }

    private IEnumerator PlayCode()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            yield break;

        isPlaying = true;
        playToken++;
        int token = playToken;

        for (int i = 0; i < code.Length; i++)
        {
            char c = code[i];
            if (c == '0')
            {
                PlayClip(shortClip, shortDuration, token);
                yield return new WaitForSeconds(shortDuration + gapDuration);
            }
            else if (c == '1')
            {
                PlayClip(longClip, longDuration, token);
                yield return new WaitForSeconds(longDuration + gapDuration);
            }
            else
            {
                yield return new WaitForSeconds(gapDuration);
            }
        }

        if (token == playToken && audioSource != null)
            audioSource.Stop();
        isPlaying = false;
    }

    private void PlayClip(AudioClip clip, float forceDuration, int token)
    {
        if (clip == null || audioSource == null)
            return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
        StartCoroutine(ForceStopAfter(forceDuration, token));
    }

    private IEnumerator ForceStopAfter(float duration, int token)
    {
        yield return new WaitForSeconds(duration);
        if (token == playToken && audioSource != null)
            audioSource.Stop();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!requireTrigger)
            return;
        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
            playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!requireTrigger)
            return;
        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
            playerInside = false;
    }

    // IInteractable
    public bool CanInteract(GameObject interactor)
    {
        if (isPlaying)
            return false;
        if (requireTrigger && !playerInside)
            return false;
        return true;
    }

    public void Interact(GameObject interactor)
    {
        PlayFromRay();
    }

    public void Highlight(bool on, GameObject interactor)
    {
        // görsel yok
    }
}
