using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basit ileri-geri hareket eden platform. İstenirse oyuncu üzerine basana kadar bekler.
/// </summary>
public class MovingPlatform : MonoBehaviour
{
    [Header("Hareket")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private bool pingPong = true;
    [SerializeField] private bool loop = false;

    [Header("Bekleme")]
    [SerializeField] private float waitAtPoint = 0.5f;

    [Header("Aktivasyon")]
    [Tooltip("Açıkken platform oyuncu üzerine basana kadar hareket etmez.")]
    [SerializeField] private bool requirePlayerToStart = false;
    [SerializeField] private string activationTag = "Player";

    [Header("Opsiyonel")]
    [Tooltip("Platform konumuna offset eklemek isterseniz kullanın.")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private string passengerTag = "Player";

    [Header("Başlangıç")]
    [Tooltip("Sadece oyunun başında platformun konumlanacağı özel nokta. Boşsa ilk waypoint'ten başlar.")]
    [SerializeField] private Transform startOverride;
    [SerializeField] private bool useStartOverride = true;

    private int currentIndex;
    private bool forward = true;
    private float waitTimer;
    private Vector3[] waypointPositions;
    private Vector3 previousPosition;
    private readonly HashSet<Transform> passengers = new HashSet<Transform>();
    private bool activated;

    void Awake()
    {
        CacheWaypoints();
        SnapToStart();
        activated = !requirePlayerToStart;
    }

    void OnValidate()
    {
        moveSpeed = Mathf.Max(0.01f, moveSpeed);
        waitAtPoint = Mathf.Max(0f, waitAtPoint);
        CacheWaypoints();
        SnapToStart();
    }

    void Update()
    {
        if (waypointPositions == null || waypointPositions.Length < 2)
            return;

        previousPosition = transform.position;

        if (!activated)
            return;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        Vector3 target = waypointPositions[currentIndex] + positionOffset;
        Vector3 toTarget = target - transform.position;
        float step = moveSpeed * Time.deltaTime;

        if (toTarget.magnitude <= step)
        {
            transform.position = target;
            SetNextTarget();
        }
        else
        {
            transform.position += toTarget.normalized * step;
        }

        Vector3 delta = transform.position - previousPosition;
        ApplyDeltaToPassengers(delta);
    }

    private void CacheWaypoints()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            waypointPositions = null;
            return;
        }

        waypointPositions = new Vector3[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            waypointPositions[i] = waypoints[i] != null ? waypoints[i].position : transform.position;
        }
    }

    private void SnapToStart()
    {
        if (waypointPositions == null || waypointPositions.Length == 0)
            return;

        currentIndex = waypointPositions.Length > 1 ? 1 : 0;
        forward = true;
        if (useStartOverride && startOverride != null)
            transform.position = startOverride.position + positionOffset;
        else
            transform.position = waypointPositions[0] + positionOffset;
        previousPosition = transform.position;
    }

    private void SetNextTarget()
    {
        waitTimer = waitAtPoint;

        if (pingPong)
        {
            if (forward)
            {
                currentIndex++;
                if (currentIndex >= waypointPositions.Length)
                {
                    currentIndex = waypointPositions.Length - 2;
                    forward = false;
                }
            }
            else
            {
                currentIndex--;
                if (currentIndex < 0)
                {
                    currentIndex = 1;
                    forward = true;
                }
            }
        }
        else if (loop)
        {
            currentIndex = (currentIndex + 1) % waypointPositions.Length;
        }
        else
        {
            currentIndex++;
            if (currentIndex >= waypointPositions.Length)
                currentIndex = waypointPositions.Length - 1;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            Transform wp = waypoints[i];
            if (wp == null)
                continue;

            Gizmos.DrawSphere(wp.position, 0.1f);

            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                Gizmos.DrawLine(wp.position, waypoints[i + 1].position);
        }
    }

    private void ApplyDeltaToPassengers(Vector3 delta)
    {
        if (passengers.Count == 0)
            return;

        var toRemove = new List<Transform>();
        foreach (Transform rider in passengers)
        {
            if (rider == null)
            {
                toRemove.Add(rider);
                continue;
            }

            rider.position += new Vector3(delta.x, 0f, 0f);
        }

        for (int i = 0; i < toRemove.Count; i++)
            passengers.Remove(toRemove[i]);
    }

    private void TryActivate(Collider2D other)
    {
        if (!requirePlayerToStart || activated)
            return;

        if (other != null && other.CompareTag(activationTag))
        {
            activated = true;
            waitTimer = 0f;
        }
    }

    private void TryAddPassenger(Collider2D other)
    {
        if (other == null || !other.CompareTag(passengerTag))
            return;

        passengers.Add(other.transform);
        TryActivate(other);
    }

    private void TryRemovePassenger(Collider2D other)
    {
        if (other == null)
            return;

        passengers.Remove(other.transform);
    }

    void OnCollisionEnter2D(Collision2D collision) => TryAddPassenger(collision.collider);
    void OnCollisionStay2D(Collision2D collision) => TryAddPassenger(collision.collider);
    void OnCollisionExit2D(Collision2D collision) => TryRemovePassenger(collision.collider);
    void OnTriggerEnter2D(Collider2D other) => TryAddPassenger(other);
    void OnTriggerExit2D(Collider2D other) => TryRemovePassenger(other);
}
