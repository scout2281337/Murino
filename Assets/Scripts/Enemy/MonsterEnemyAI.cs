using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(NavMeshAgent))]
public class MonsterEnemyAI : MonoBehaviour
{
    private enum MonsterState
    {
        Patrol,
        Suspicious,
        Investigate,
        Search,
        Chase,
        Attack
    }

    [Header("References")]
    [SerializeField] private HorrorPlayerController player;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private Transform eyePoint;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float patrolSpeed = 1.8f;
    [SerializeField, Min(0f)] private float investigateSpeed = 2.6f;
    [SerializeField, Min(0f)] private float chaseSpeed = 4.2f;
    [SerializeField, Min(0f)] private float patrolPointTolerance = 0.45f;
    [SerializeField, Min(0f)] private float repathInterval = 0.18f;

    [Header("Sight")]
    [SerializeField, Min(0f)] private float sightRange = 14f;
    [SerializeField, Range(1f, 180f)] private float fieldOfView = 95f;
    [SerializeField, Min(0f)] private float eyeHeight = 1.65f;
    [SerializeField] private LayerMask sightBlockMask = ~0;
    [SerializeField, Min(0f)] private float suspicionGainPerSecond = 1.3f;
    [SerializeField, Min(0f)] private float suspicionDecayPerSecond = 0.45f;
    [SerializeField, Range(0.05f, 1f)] private float chaseSuspicionThreshold = 1f;

    [Header("Hearing")]
    [SerializeField, Min(0f)] private float baseHearingRange = 8f;
    [SerializeField, Min(0f)] private float sprintHearingBonus = 6f;
    [SerializeField, Min(0f)] private float hearingMemoryDuration = 4f;

    [Header("Search")]
    [SerializeField, Min(0f)] private float investigateWaitTime = 1.2f;
    [SerializeField, Min(0f)] private float searchDuration = 6f;
    [SerializeField, Min(0f)] private float searchRadius = 4f;
    [SerializeField, Min(0f)] private float lostPlayerMemory = 3.5f;

    [Header("Attack")]
    [SerializeField, Min(0f)] private float attackRange = 1.45f;
    [SerializeField, Min(0f)] private float attackCooldown = 1.4f;
    [SerializeField] private UnityEvent onAttackPlayer;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    public bool HasLineOfSight => hasLineOfSight;
    public bool IsChasing => state == MonsterState.Chase;
    public float SuspicionNormalized => Mathf.Clamp01(suspicion);

    private NavMeshAgent agent;
    private MonsterState state = MonsterState.Patrol;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 lastHeardPosition;
    private float lastSawPlayerTime = -999f;
    private float lastHeardPlayerTime = -999f;
    private float nextRepathTime;
    private float stateTimer;
    private float suspicion;
    private float nextAttackTime;
    private int patrolIndex;
    private bool hasLineOfSight;
    private readonly RaycastHit[] sightHits = new RaycastHit[12];

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            player = FindFirstObjectByType<HorrorPlayerController>();
        }

        if (eyePoint == null)
        {
            eyePoint = transform;
        }
    }

    private void OnEnable()
    {
        EnterState(MonsterState.Patrol);
    }

    private void Update()
    {
        if (player == null || !agent.isOnNavMesh)
        {
            return;
        }

        UpdateSenses();
        TickState();
    }

    private void UpdateSenses()
    {
        bool canSeePlayer = CanSeePlayer();
        hasLineOfSight = canSeePlayer;

        if (canSeePlayer)
        {
            lastKnownPlayerPosition = player.transform.position;
            lastSawPlayerTime = Time.time;
            float distanceFactor = 1f - Mathf.Clamp01(Vector3.Distance(GetEyePosition(), GetPlayerFocusPosition()) / sightRange);
            suspicion += suspicionGainPerSecond * Mathf.Lerp(0.45f, 1f, distanceFactor) * player.StealthVisibilityMultiplier * Time.deltaTime;
        }
        else
        {
            suspicion -= suspicionDecayPerSecond * Time.deltaTime;
        }

        suspicion = Mathf.Clamp01(suspicion);

        if (CanHearPlayer())
        {
            lastHeardPosition = player.transform.position;
            lastKnownPlayerPosition = lastHeardPosition;
            lastHeardPlayerTime = Time.time;

            if (state == MonsterState.Patrol || state == MonsterState.Search)
            {
                EnterState(MonsterState.Suspicious);
            }
        }

        if (suspicion >= chaseSuspicionThreshold && state != MonsterState.Attack)
        {
            EnterState(MonsterState.Chase);
        }
    }

    private void TickState()
    {
        stateTimer += Time.deltaTime;

        switch (state)
        {
            case MonsterState.Patrol:
                TickPatrol();
                break;
            case MonsterState.Suspicious:
                TickSuspicious();
                break;
            case MonsterState.Investigate:
                TickInvestigate();
                break;
            case MonsterState.Search:
                TickSearch();
                break;
            case MonsterState.Chase:
                TickChase();
                break;
            case MonsterState.Attack:
                TickAttack();
                break;
        }
    }

    private void TickPatrol()
    {
        agent.speed = patrolSpeed;

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            agent.ResetPath();
            return;
        }

        if (!agent.hasPath)
        {
            SetDestination(patrolPoints[patrolIndex].position);
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            SetDestination(patrolPoints[patrolIndex].position);
        }
    }

    private void TickSuspicious()
    {
        agent.speed = investigateSpeed;

        if (Time.time - lastHeardPlayerTime <= hearingMemoryDuration)
        {
            SetDestination(lastHeardPosition);
        }

        if (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance)
        {
            EnterState(MonsterState.Investigate);
        }
    }

    private void TickInvestigate()
    {
        agent.ResetPath();
        LookAtFlat(lastKnownPlayerPosition);

        if (stateTimer >= investigateWaitTime)
        {
            EnterState(MonsterState.Search);
        }
    }

    private void TickSearch()
    {
        agent.speed = investigateSpeed;

        if (stateTimer >= searchDuration)
        {
            suspicion = 0f;
            EnterState(MonsterState.Patrol);
            return;
        }

        if (!agent.hasPath || (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance))
        {
            SetDestination(GetSearchPoint());
        }
    }

    private void TickChase()
    {
        agent.speed = chaseSpeed;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= attackRange)
        {
            EnterState(MonsterState.Attack);
            return;
        }

        if (Time.time >= nextRepathTime)
        {
            Vector3 chaseTarget = hasLineOfSight || Time.time - lastSawPlayerTime <= lostPlayerMemory
                ? player.transform.position
                : lastKnownPlayerPosition;

            SetDestination(chaseTarget);
            nextRepathTime = Time.time + repathInterval;
        }

        if (!hasLineOfSight && Time.time - lastSawPlayerTime > lostPlayerMemory)
        {
            EnterState(MonsterState.Search);
        }
    }

    private void TickAttack()
    {
        agent.ResetPath();
        LookAtFlat(player.transform.position);

        if (Vector3.Distance(transform.position, player.transform.position) > attackRange * 1.25f)
        {
            EnterState(MonsterState.Chase);
            return;
        }

        if (Time.time >= nextAttackTime)
        {
            onAttackPlayer.Invoke();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void EnterState(MonsterState nextState)
    {
        if (state == nextState)
        {
            return;
        }

        state = nextState;
        stateTimer = 0f;

        if (state == MonsterState.Search)
        {
            SetDestination(GetSearchPoint());
        }
    }

    private bool CanSeePlayer()
    {
        Vector3 eyePosition = GetEyePosition();
        Vector3 targetPosition = GetPlayerFocusPosition();
        Vector3 toPlayer = targetPosition - eyePosition;
        float distance = toPlayer.magnitude;
        float effectiveSightRange = sightRange * player.StealthVisibilityMultiplier;

        if (distance > effectiveSightRange)
        {
            return false;
        }

        if (Vector3.Angle(transform.forward, toPlayer) > fieldOfView * 0.5f)
        {
            return false;
        }

        int hitCount = Physics.RaycastNonAlloc(eyePosition, toPlayer.normalized, sightHits, distance, sightBlockMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Transform hitTransform = sightHits[i].transform;

            if (hitTransform == null)
            {
                continue;
            }

            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                continue;
            }

            if (hitTransform == player.transform || hitTransform.IsChildOf(player.transform))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private bool CanHearPlayer()
    {
        float noise = player.NoiseMultiplier;

        if (noise <= 0f)
        {
            return false;
        }

        float hearingRange = baseHearingRange * noise;

        if (player.IsSprinting)
        {
            hearingRange += sprintHearingBonus;
        }

        return Vector3.Distance(transform.position, player.transform.position) <= hearingRange;
    }

    private Vector3 GetEyePosition()
    {
        if (eyePoint != null && eyePoint != transform)
        {
            return eyePoint.position;
        }

        return transform.position + Vector3.up * eyeHeight;
    }

    private Vector3 GetPlayerFocusPosition()
    {
        Transform target = player.CameraRoot != null ? player.CameraRoot : player.transform;
        return target.position;
    }

    private Vector3 GetSearchPoint()
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * searchRadius;
            Vector3 candidate = lastKnownPlayerPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return lastKnownPlayerPosition;
    }

    private void SetDestination(Vector3 destination)
    {
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void LookAtFlat(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 8f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Vector3 eyePosition = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * eyeHeight;
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, baseHearingRange);
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, sightRange);

        Vector3 leftRay = Quaternion.Euler(0f, -fieldOfView * 0.5f, 0f) * transform.forward;
        Vector3 rightRay = Quaternion.Euler(0f, fieldOfView * 0.5f, 0f) * transform.forward;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(eyePosition, eyePosition + leftRay * sightRange);
        Gizmos.DrawLine(eyePosition, eyePosition + rightRay * sightRange);
    }
}
