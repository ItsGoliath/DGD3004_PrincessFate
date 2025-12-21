using UnityEngine;

/// <summary>
/// Hinge tabanlı döner platformları dengelemek için hız limiti, sönümleme, yay ve opsiyonel tilt clamp uygular.
/// İsteğe bağlı olarak oyuncu temas edene kadar yüksek sönüm uygular, temas sonrası normal değere döner.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class HingeStabilizer2D : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [Tooltip("Angular velocity limit (deg/sec). 0 = limitsiz.")]
    [SerializeField] private float maxAngularVelocityDeg = 180f;

    [Header("Sönümleme")]
    [Tooltip("Normal ekstra sönüm (temas sonrası). 0 = devre dışı.")]
    [SerializeField] private float angularDampFactor = 2f;
    [Tooltip("Oyuncu gelene kadar uygulanacak ekstra sönüm. Yüksek tutmak köprüyü sabit tutar.")]
    [SerializeField] private float inactiveAngularDampFactor = 50f;
    [Tooltip("Oyuncu temas edince geçilecek ekstra sönüm.")]
    [SerializeField] private float activeAngularDampFactor = 2f;

    [Tooltip("Light spring force toward targetAngleDeg. 0 = disabled.")]
    [SerializeField] private float springStrength = 0f;
    [Tooltip("Resting angle in degrees.")]
    [SerializeField] private float targetAngleDeg = 0f;
    [Tooltip("If true, clamps tilt around targetAngleDeg using maxTiltDeg.")]
    [SerializeField] private bool useTiltClamp = false;
    [Tooltip("Max allowed tilt from target angle when clamp is enabled.")]
    [SerializeField] private float maxTiltDeg = 80f;

    [Header("Aktivasyon")]
    [Tooltip("True ise oyuncu tetikleyene kadar yüksek sönümde kalır.")]
    [SerializeField] private bool requirePlayerToActivate = false;
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Aktivasyon için kullanılacak trigger/collider. Boşsa aynı nesnedeki collider kullanılır.")]
    [SerializeField] private Collider2D activationCollider;

    private bool activatedOnce;
    private float configuredMaxAngularVelocity;
    private float configuredActiveDamp;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        activationCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        configuredMaxAngularVelocity = maxAngularVelocityDeg;
        configuredActiveDamp = angularDampFactor > 0f ? angularDampFactor : activeAngularDampFactor;

        if (requirePlayerToActivate)
        {
            angularDampFactor = inactiveAngularDampFactor;
        }
        else
        {
            angularDampFactor = activeAngularDampFactor > 0f ? activeAngularDampFactor : configuredActiveDamp;
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (maxAngularVelocityDeg > 0f)
        {
            float maxRad = maxAngularVelocityDeg * Mathf.Deg2Rad;
            rb.angularVelocity = Mathf.Clamp(rb.angularVelocity, -maxRad, maxRad);
        }

        if (angularDampFactor > 0f)
        {
            rb.angularVelocity *= Mathf.Exp(-angularDampFactor * Time.fixedDeltaTime);
        }

        if (springStrength > 0f)
        {
            float current = rb.rotation;
            float delta = Mathf.DeltaAngle(current, targetAngleDeg);
            rb.AddTorque(delta * springStrength * Mathf.Deg2Rad, ForceMode2D.Force);
        }

        if (useTiltClamp && maxTiltDeg > 0f)
        {
            float currentAngle = rb.rotation;
            float deltaFromTarget = Mathf.DeltaAngle(targetAngleDeg, currentAngle);
            float clampedDelta = Mathf.Clamp(deltaFromTarget, -maxTiltDeg, maxTiltDeg);

            if (!Mathf.Approximately(deltaFromTarget, clampedDelta))
            {
                float newAngle = targetAngleDeg + clampedDelta;
                rb.MoveRotation(newAngle);
                rb.angularVelocity = 0f;
            }
        }
    }

    
    private void OnCollisionEnter2D(Collision2D collision) => TryActivate(collision.collider);
    
    private void OnCollisionStay2D(Collision2D collision) => TryActivate(collision.collider);

    private void TryActivate(Collider2D col)
    {
        if (!requirePlayerToActivate || activatedOnce || col == null)
            return;

        // Aktivasyon collider'ı atanmışsa, yalnızca o collider ya da player tag'li collider kabul edilir.
        if (activationCollider != null && col != activationCollider && !col.CompareTag(playerTag))
            return;

        if (col.CompareTag(playerTag))
        {
            activatedOnce = true;
            SetActivated(true);
        }
    }

    private void SetActivated(bool enabled)
    {
        if (rb == null) return;

        rb.angularVelocity = 0f;
        angularDampFactor = enabled ? (activeAngularDampFactor > 0f ? activeAngularDampFactor : configuredActiveDamp)
                                     : inactiveAngularDampFactor;
        if (!enabled)
        {
            rb.rotation = targetAngleDeg;
        }
    }
}


