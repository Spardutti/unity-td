using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private int goldReward = 10;
    [SerializeField] private int attackDamage = 1;

    [Header("Visual Settings")]
    [SerializeField] private Color enemyColor = Color.red;
    [SerializeField] private Vector3 enemySize = Vector3.one;

    [Header("Movement")]
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private bool faceMovementDirection = true;

    [Header("Debug Info")]
    [SerializeField] private bool showDebugInfo = true;


    private float currentHeath;
    private PathData assignedPath;
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
    public int AttackDamage => attackDamage;

    void Awake()
    {
        currentHeath = maxHealth;

        enemyRenderer = GetComponent<Renderer>();
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

    }

    public void ApplyHealthMultiplier(float multiplier)
    {
        maxHealth *= multiplier;
        currentHeath = maxHealth;
        Debug.Log($"Enemy: Applied health multiplier {multiplier}, new health: {maxHealth}");
    }

    public void SetPath(PathData path)
    {
        assignedPath = path;
        if (assignedPath != null && showDebugInfo)
        {
            Debug.Log($"Enemy: Set path {assignedPath.pathName} with {assignedPath.WaypointCount} waypoints");
        }
    }

    public PathData GetAssignedPath()
    {
        return assignedPath;
    }

    private void StartMovement()
    {
        if (assignedPath == null || assignedPath.WaypointCount == 0)
        {
            Debug.LogError("Enemy: PathManager has no waypoints");
            return;
        }

        Waypoint startWaypoint = assignedPath.GetStartWaypoint();
        if (startWaypoint != null)
        {
            transform.position = startWaypoint.Position;
            currentWaypointIndex = 0;
            MoveToNextWaypoint();
        }
    }

    private void MoveToNextWaypoint()
    {
        if (currentWaypointIndex >= assignedPath.WaypointCount)
        {
            ReachPathEnd();
            return;
        }
        currentTargetWaypoint = assignedPath.waypoints[currentWaypointIndex];
        if (currentTargetWaypoint != null)
        {
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


        MoveToNextWaypoint();

    }

    private void ReachPathEnd()
    {

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

        // Award gold to player
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddGold(goldReward);
        }
        else
        {
            Debug.LogError("Enemy: EconomyManager not found");
        }

        DestroyEnemy();
    }

    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }

    public float GetPathProgress()
    {
        if (assignedPath == null) return 0f;

        return (float)currentWaypointIndex / assignedPath.WaypointCount;
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


}
