using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private int goldReward = 10;

    [Header("Visual Settings")]
    [SerializeField] private Color enemyColor = Color.red;
    [SerializeField] private Vector3 enemySize = Vector3.one;

    [Header("Movement")]
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private bool faceMovementDirection = true;


    private float currentHeath;
    private PathManager pathManager;
    private Waypoint currentTargetWaypoint;
    private int currentWaypointIndex;
    private bool isMoving = false;
    private Renderer enemyRenderer;

    // external access
    public float CurrentHealth => currentHeath;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHeath / maxHealth;
    public bool IsAlive => currentHeath > 0;
    public int GoldReward => goldReward;

    void Awake()
    {
        currentHeath = maxHealth;

        enemyRenderer = GetComponent<Renderer>();

        pathManager = FindFirstObjectByType<PathManager>();

        if (pathManager == null)
        {
            Debug.LogError("Enemy: PathManager not found");
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        SetupVisuals();
        StartMovement();
    }

    private void SetupVisuals()
    {
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = enemyColor;
        }
        transform.localScale = enemySize;

        Debug.Log($"Enemy spawned with {maxHealth} HP");
    }

    private void StartMovement()
    {
        if (pathManager == null || pathManager.WaypointCount == 0)
        {
            Debug.LogError("Enemy: PathManager has no waypoints");
            return;
        }

        Waypoint startWaypoint = pathManager.GetStartWaypoint();
        if (startWaypoint != null)
        {
            Debug.Log($"Enemy: Starting at waypoint {startWaypoint.Order}");
            transform.position = startWaypoint.Position;
            currentWaypointIndex = 0;
            MoveToNextWaypoint();
        }
    }

    private void MoveToNextWaypoint()
    {
        if (currentWaypointIndex >= pathManager.WaypointCount)
        {
            Debug.Log($"Enemy: Reached end of path");
            ReachPathEnd();
            return;
        }
        Debug.Log($"Enemy: Moving to waypoint {currentWaypointIndex + 1}");
        currentTargetWaypoint = pathManager.Waypoints[currentWaypointIndex];
        if (currentTargetWaypoint != null)
        {
            Debug.Log($"Enemy: Moving to waypoint {currentTargetWaypoint.Order}");
            StartCoroutine(MoveToWaypoint(currentTargetWaypoint));
        }
    }

    private IEnumerator MoveToWaypoint(Waypoint targetWaypoint)
    {
        isMoving = true;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = targetWaypoint.Position;

        float distance = Vector3.Distance(startPosition, targetPosition);
        float journeyTime = distance / moveSpeed;

        float elapsedTime = 0f;

        Debug.Log($"Enemy moving to waypoint {targetWaypoint.Order} - Distance: {distance} - time: {journeyTime:F2}s");

        while (elapsedTime < journeyTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / journeyTime;

            transform.position = Vector3.Lerp(startPosition, targetPosition, progress);

            if (faceMovementDirection)
            {
                Vector3 direction = (targetPosition - startPosition).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }

            yield return null;
        }

        transform.position = targetPosition;

        currentWaypointIndex++;
        isMoving = false;

        Debug.Log($"Enemy reached waypoint {targetWaypoint.Order}");

        MoveToNextWaypoint();

    }

    private void ReachPathEnd()
    {
        Debug.Log(" Enemy reached end of path");

        DestroyEnemy();
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        currentHeath -= damage;
        currentHeath = Mathf.Max(0, currentHeath);

        Debug.Log($"Enemy took {damage} damage, now at {currentHeath}/{maxHealth}");

        StartCoroutine(DamageFlash());

        if (!IsAlive)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"Enemy died! Reward: {goldReward}");

        DestroyEnemy();
    }

    private void DestroyEnemy()
    {
        Debug.Log($"Enemy destroyed");
        Destroy(gameObject);
    }

    public float GetPathProgress()
    {
        if (pathManager == null) return 0f;

        return (float)currentWaypointIndex / pathManager.WaypointCount;
    }

    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Vector3 healthBarPos = transform.position + Vector3.up * 1.5f;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(healthBarPos, healthBarPos + Vector3.right * (HealthPercentage * 2f));
        }
    }

    private IEnumerator DamageFlash()
    {
        if (enemyRenderer != null)
        {
            Color originalColor = enemyRenderer.material.color;
            enemyRenderer.material.color = Color.red;

            yield return new WaitForSeconds(0.1f);

            enemyRenderer.material.color = originalColor;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
