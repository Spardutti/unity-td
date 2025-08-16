using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float travelSpeed = 15f;
    [SerializeField] private float maxLifeTime = 5f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject impactEffect;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private Vector3 targetPosition;
    private bool isFiring = false;
    private float spawnTime;

    void Start()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        MoveToTarget();
        CheckLifeTime();
    }

    private void MoveToTarget()
    {
        // Only move if we're actually firing
        if (!isFiring) return;

        // Calculate movement with deltaTime clamping to prevent large jumps
        float clampedDeltaTime = Mathf.Min(Time.deltaTime, 0.02f);
        float moveDistance = travelSpeed * clampedDeltaTime;

        // Move toward target position
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveDistance
        );

        // Check if we've arrived at target
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget < 0.05f)
        {
            OnArrival();
        }
    }

    private void CheckLifeTime()
    {
        // Safety: Destroy if projectile is too old
        if (Time.time - spawnTime > maxLifeTime)
        {
            if (debugMode) Debug.Log("Projectile: Destroying old projectile");
            Destroy(gameObject);
        }
    }

    private void OnArrival()
    {
        if (debugMode) Debug.Log("Projectile: Arrived at target");

        // Play impact effect if assigned
        if (impactEffect != null)
        {
            Instantiate(impactEffect, targetPosition, Quaternion.identity);
        }

        // Destroy the visual projectile
        Destroy(gameObject);
    }

    public void FireToPosition(Vector3 destination)
    {
        targetPosition = destination;
        isFiring = true;

        // Face the target direction
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        if (debugMode)
        {
            float distance = Vector3.Distance(transform.position, targetPosition);
            Debug.Log($"Projectile: Firing to target, distance: {distance:F2} units");
        }
    }

    void OnDrawGizmos()
    {
        if (debugMode && isFiring)
        {
            // Draw line to target in Scene view
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }

    // Testing methods - can be removed in final build
    [ContextMenu("Test Fire Forward")]
    private void TestFireForward()
    {
        Vector3 testTarget = transform.position + Vector3.forward * 5f;
        FireToPosition(testTarget);
    }

    [ContextMenu("Test Fire Visible")]
    private void TestFireVisible()
    {
        Vector3 visibleTarget = new Vector3(10f, 1f, 0f);
        FireToPosition(visibleTarget);
    }
}