using System.Collections;
using UnityEngine;

/// <summary>
/// Basıldığı anda diken görselini aktif edip belirli bir süre sonra geri kapatan basit tuzak.
/// </summary>
public class SpikeTrap : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private Collider2D triggerCollider;
    [SerializeField] private GameObject spikesRoot;

    [Header("Ayarlar")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float activationDelay = 2f;
    [SerializeField] private float activeDuration = 3f;
    [SerializeField] private bool loopAutomatically = false;
    [SerializeField] private float loopInterval = 4f;
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float damageCooldown = 0.5f;

    private Coroutine activeRoutine;
    private bool spikesActive;
    private float lastDamageTime = float.NegativeInfinity;

    void Awake()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider2D>();

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;

        SetSpikesActive(false);
    }

    void Start()
    {
        if (loopAutomatically)
            StartCoroutine(AutoLoopRoutine());
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (loopAutomatically)
            return;

        if (!other.CompareTag(playerTag))
            return;

        ActivateSpikes();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!spikesActive)
            return;

        TryDamagePlayer(other);
    }

    private void ActivateSpikes(bool force = false)
    {
        if (!force && activeRoutine != null)
            return;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(ActivationSequence());
    }

    private IEnumerator ActivationSequence()
    {
        if (activationDelay > 0f)
            yield return new WaitForSeconds(activationDelay);

        SetSpikesActive(true);

        yield return new WaitForSeconds(activeDuration);
        SetSpikesActive(false);
        activeRoutine = null;
    }

    private IEnumerator AutoLoopRoutine()
    {
        while (true)
        {
            ActivateSpikes(true);
            float wait = Mathf.Max(loopInterval, activationDelay + activeDuration);
            yield return new WaitForSeconds(wait);
        }
    }

    private void SetSpikesActive(bool active)
    {
        spikesActive = active;
        if (spikesRoot != null)
            spikesRoot.SetActive(active);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (Time.time < lastDamageTime + damageCooldown)
            return;

        Player player = other.GetComponent<Player>();
        if (player == null)
            return;

        player.TakeDamage(damageAmount, transform.position);
        lastDamageTime = Time.time;
    }
}
