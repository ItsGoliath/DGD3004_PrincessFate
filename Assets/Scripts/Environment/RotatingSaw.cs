using UnityEngine;

/// <summary>
/// Basit testere engeli: verilen rotasyon ekseninde sürekli döner ve isteğe bağlı olarak
/// belirlenen noktalar arasında hareket eder.
/// </summary>
public class RotatingSaw : MonoBehaviour
{
    [Header("Dönüş")]
    public float spinSpeed = 360f; // derece/sn
    public Vector3 spinAxis = Vector3.forward; // Varsayılan: Z ekseni

    [Header("Yörünge (İsteğe Bağlı)")]
    public Transform[] waypoints;
    public float moveSpeed = 3f;
    public bool loop = true;

    [Header("Hasar")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float damageCooldown = 0.5f;

    private float lastDamageTime = float.NegativeInfinity;

    private int currentIndex;
    private bool forward = true;

    void Update()
    {
        RotateSaw();
        MoveAlongPath();
    }

    private void RotateSaw()
    {
        if (spinSpeed == 0f)
            return;

        transform.Rotate(spinAxis * spinSpeed * Time.deltaTime, Space.Self);
    }

    private void MoveAlongPath()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Transform target = waypoints[currentIndex];
        if (target == null)
            return;

        transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) <= 0.01f)
            AdvanceWaypoint();
    }

    private void AdvanceWaypoint()
    {
        if (waypoints.Length <= 1)
            return;

        if (loop)
        {
            currentIndex = (currentIndex + 1) % waypoints.Length;
            return;
        }

        if (forward)
        {
            if (currentIndex >= waypoints.Length - 1)
            {
                forward = false;
                currentIndex--;
            }
            else
            {
                currentIndex++;
            }
        }
        else
        {
            if (currentIndex <= 0)
            {
                forward = true;
                currentIndex++;
            }
            else
            {
                currentIndex--;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (other == null || !other.CompareTag(playerTag))
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
