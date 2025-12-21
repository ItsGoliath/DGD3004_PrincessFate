using UnityEngine;

/// <summary>
/// Sabit tuzak: Belirli bir menzilde oyuncu algilaninca ok firlatir (2D).
/// </summary>
public class ArrowTrap2D : MonoBehaviour
{
    [Header("Algilama")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private LayerMask lineOfSightMask = ~0;
    [SerializeField] private Vector2 detectionBoxSize = new Vector2(6f, 2f);
    [SerializeField] private Vector2 detectionBoxOffset = Vector2.zero;

    [Header("Atesleme")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float arrowSpeed = 12f;
    [SerializeField] private float fireCooldown = 1.5f;
    [SerializeField] private bool faceRight = true;

    private float cooldownTimer;

    private void Reset()
    {
        firePoint = transform;
    }

    private void Update()
    {
        if (arrowPrefab == null || firePoint == null)
            return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f)
            return;

        Transform target = FindTargetInRange();
        if (target != null && HasLineOfSight(target))
        {
            FireArrow();
            cooldownTimer = fireCooldown;
        }
    }

    private Transform FindTargetInRange()
    {
        GameObject targetObj = GameObject.FindGameObjectWithTag(targetTag);
        if (targetObj == null)
            return null;

        float dist = Vector2.Distance(firePoint.position, targetObj.transform.position);
        return dist <= detectionRange ? targetObj.transform : null;
    }

    private bool HasLineOfSight(Transform target)
    {
        Vector2 dir = (target.position - firePoint.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(firePoint.position, dir, detectionRange, lineOfSightMask);
        if (hit.collider == null)
            return false;
        return hit.collider.transform == target || hit.collider.CompareTag(targetTag);
    }

    private void FireArrow()
    {
        GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);
        float dirX = faceRight ? 1f : -1f;
        Vector2 velocity = new Vector2(dirX * arrowSpeed, 0f);

        Rigidbody2D rb2d = arrow.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.linearVelocity = velocity;
        }
        else
        {
            arrow.transform.position += (Vector3)velocity * Time.deltaTime;
        }

        arrow.transform.localScale = new Vector3(Mathf.Sign(dirX) * Mathf.Abs(arrow.transform.localScale.x), arrow.transform.localScale.y, arrow.transform.localScale.z);
    }

    private void OnDrawGizmosSelected()
    {
        Transform fp = firePoint != null ? firePoint : transform;
        Gizmos.color = Color.yellow;
        float dir = faceRight ? 1f : -1f;
        Vector3 center = fp.position;
        center += (Vector3)new Vector2(dir * (detectionBoxSize.x * 0.5f), 0f);
        center += (Vector3)new Vector2(detectionBoxOffset.x * dir, detectionBoxOffset.y);
        Vector3 size = new Vector3(detectionBoxSize.x, detectionBoxSize.y, 0.1f);
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(fp.position, fp.position + Vector3.right * dir * detectionRange);
    }
}
