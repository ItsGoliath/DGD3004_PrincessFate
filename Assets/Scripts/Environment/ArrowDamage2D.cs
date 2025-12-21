using System.Reflection;
using UnityEngine;

/// <summary>
/// Ok davran���: �arpt���nda hasar verir ve yok olur; kamera a��s�ndan ��k�nca yok olur.
/// Rigidbody2D h�z�na g�re iste�e ba�l� flip yapar.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ArrowDamage2D : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool destroyOnAnyHit = true;
    [SerializeField] private float lifeTime = 5f; // g�venlik i�in

    [Header("G�rsel Flip")]
    [SerializeField] private bool flipSpriteWithVelocity = true;
    [SerializeField] private SpriteRenderer spriteToFlip; // Child sprite varsa buraya atay�n

    private Rigidbody2D rb;
    private Vector3 initialScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (lifeTime > 0f)
            Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (!flipSpriteWithVelocity || rb == null)
            return;

        float vx = rb.linearVelocity.x;
        if (Mathf.Abs(vx) > 0.01f)
        {
            if (spriteToFlip != null)
            {
                // SpriteRenderer.flipX kullan
                if (vx < 0f) spriteToFlip.flipX = true;
                else if (vx > 0f) spriteToFlip.flipX = false;
            }
            else
            {
                Vector3 s = initialScale;
                s.x = Mathf.Sign(vx) * Mathf.Abs(s.x);
                transform.localScale = s;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.gameObject);
    }

    private void HandleHit(GameObject other)
    {
        if (other.CompareTag(targetTag))
        {
            TryApplyDamage(other);
        }

        if (destroyOnAnyHit)
            Destroy(gameObject);
    }

    private void TryApplyDamage(GameObject target)
    {
        const string methodName = "TakeDamage";
        var behaviours = target.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            var b = behaviours[i];
            if (b == null) continue;
            var type = b.GetType();
            var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) continue;

            var parameters = method.GetParameters();
            try
            {
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int))
                {
                    method.Invoke(b, new object[] { damage });
                    return;
                }
                if (parameters.Length == 2)
                {
                    object p1 = damage;
                    object p2 = null;
                    var pType = parameters[1].ParameterType;
                    if (pType == typeof(Vector2)) p2 = (Vector2)transform.position;
                    else if (pType == typeof(Vector3)) p2 = transform.position;
                    else if (pType == typeof(GameObject)) p2 = gameObject;
                    method.Invoke(b, new object[] { p1, p2 });
                    return;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"ArrowDamage2D: TakeDamage �a�r�s� ba�ar�s�z ({ex.Message})", this);
            }
        }

        target.SendMessage(methodName, damage, SendMessageOptions.DontRequireReceiver);
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}
