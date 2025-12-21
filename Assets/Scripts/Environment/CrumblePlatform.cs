using System.Collections;
using UnityEngine;

/// <summary>
/// Üzerine basıldığında belirli süre titreşip düşen ve bir süre sonra geri gelen platform.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CrumblePlatform : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private Collider2D platformCollider;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform shakeVisual;

    [Header("Ayarlar")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float shakeDuration = 2f;
    [SerializeField] private float shakeIntensity = 0.1f;
    [SerializeField] private float respawnDelay = 5f;

    [Tooltip("Aktifleştirildiğinde platform tekrar kullanılabilir hale gelir.")]
    [SerializeField] private bool resetOnRespawn = true;

    private Vector3 initialVisualLocalPosition;
    private bool isShaking;
    private bool isCrumbled;
    private Coroutine crumbleRoutine;

    void Awake()
    {
        if (platformCollider == null)
            platformCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (shakeVisual == null)
            shakeVisual = spriteRenderer != null ? spriteRenderer.transform : transform;

        initialVisualLocalPosition = shakeVisual != null ? shakeVisual.localPosition : Vector3.zero;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag(playerTag))
            TryStartCrumble();
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag(playerTag))
            TryStartCrumble();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            TryStartCrumble();
    }

    private void TryStartCrumble()
    {
        if (isShaking || isCrumbled)
            return;

        crumbleRoutine = StartCoroutine(CrumbleSequence());
    }

    private IEnumerator CrumbleSequence()
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            Vector2 shakeOffset = Random.insideUnitCircle * shakeIntensity;
            if (shakeVisual != null)
                shakeVisual.localPosition = initialVisualLocalPosition + (Vector3)shakeOffset;
            yield return null;
        }

        if (shakeVisual != null)
            shakeVisual.localPosition = initialVisualLocalPosition;
        isShaking = false;
        isCrumbled = true;
        SetPlatformActive(false);

        yield return new WaitForSeconds(respawnDelay);

        SetPlatformActive(true);
        isCrumbled = false;
        if (resetOnRespawn && shakeVisual != null)
            shakeVisual.localPosition = initialVisualLocalPosition;

        crumbleRoutine = null;
    }

    private void SetPlatformActive(bool active)
    {
        if (platformCollider != null)
            platformCollider.enabled = active;

        if (spriteRenderer != null)
            spriteRenderer.enabled = active;
    }
}
