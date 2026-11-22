using UnityEngine;
using UnityEngine.AI;

// Put this on the Mutant Monster 7 root (needs a NavMeshAgent, and the monster must start on the baked NavMesh).
// Patrols until it SEES the player, chases while it can see them, attacks when close.
// After losing the player it checks the last seen spot, searches nearby spots, then goes back to patrolling.
[RequireComponent(typeof(NavMeshAgent))]
public class MonsterAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack, Search }

    [Header("References")]
    public Transform player;          // the Player cylinder
    public Transform eyes;            // empty child at head height (falls back to this transform)
    public Animator animator;         // optional

    [Header("Vision")]
    public float sightRange = 15f;
    [Range(10f, 360f)] public float fieldOfView = 110f;
    public LayerMask obstacleMask = ~0;       // layers that can block sight (walls, doors, etc.)
    public float sightMemory = 1.5f;          // keeps chasing this long after briefly losing sight
    public float closeAwarenessRadius = 3f;   // notices you from behind within this distance (needs line of sight)
    public bool chaseSeesAllAround = true;    // while chasing, ignore the field-of-view cone

    [Header("Patrol")]
    public Transform[] patrolPoints;      // optional; if empty it wanders randomly around where it started
    public float wanderRadius = 15f;
    public float patrolSpeed = 1.5f;
    public float waitAtPatrolPoint = 2f;

    [Header("Chase")]
    public float chaseSpeed = 4.5f;
    public float giveUpTime = 3f;         // gives up chasing if it can't get closer for this long (player unreachable)
    public float giveUpBlindTime = 6f;    // after giving up it ignores the player this long and searches elsewhere

    [Header("Search (after losing the player)")]
    public float searchSpeed = 2.5f;
    public int searchPointCount = 4;      // extra nearby spots to check after the last seen position
    public float searchRadius = 8f;       // how far from the last seen position it looks
    public float lookAroundTime = 3f;     // seconds spent looking around at each spot

    [Header("Attack")]
    public float attackRange = 1.8f;      // starts an attack when this close (keep above the agent's Stopping Distance)
    public float hitRange = 2.3f;         // the player must still be this close when the hit lands
    public int attackDamage = 34;
    public float attackHitDelay = 0.6f;   // seconds into the attack animation when the hit lands
    public float attackDuration = 1.5f;   // total length of the attack (match your attack clip length)
    public float attackCooldown = 0.5f;   // pause after an attack before the next one
    public float turnSpeed = 8f;          // how fast it turns to face the player during an attack

    [Header("Hiding")]
    public float hideDiscoverDistance = 2.5f;  // while searching this close to where you hide, it may check there
    [Range(0f, 1f)] public float hideDiscoverChance = 0.3f;

    [Header("Animator parameters (create these in your Animator Controller)")]
    public string speedParam = "Speed";           // Float
    public string lookAroundParam = "LookAround"; // Bool
    public string attackParam = "Attack";         // Trigger

    public State state = State.Patrol;

    // other scripts (like MonsterAudio) can listen to these
    public event System.Action OnAttackStart;
    public event System.Action OnAttackHit;

    NavMeshAgent agent;
    Collider playerCollider;
    PlayerHealth playerHealth;
    PlayerHiding hiding;

    // true while the monster can currently see the player (PlayerHiding uses this)
    public bool SeesPlayerNow { get; private set; }

    Vector3 homePosition;
    Vector3 lastSeenPosition;
    float lastSeenTime;

    int patrolIndex = -1;
    bool waiting;          // standing still at a patrol point
    float waitTimer;

    bool searchLooking;    // looking around at a search spot
    float searchTimer;
    int searchPointsLeft;

    float attackTimer;
    float nextAttackTime;
    bool hitDone;

    float stuckTimer;       // chasing but not getting closer
    float moveStuckTimer;   // has a destination but isn't moving
    float blindUntil;       // monster ignores the player until this time (after giving up)

    int speedHash, lookHash, attackHash;
    bool hasSpeedParam, hasLookParam, hasAttackParam;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (eyes == null) eyes = transform;
        if (player != null)
        {
            playerCollider = player.GetComponent<Collider>();
            playerHealth = player.GetComponent<PlayerHealth>();
            hiding = player.GetComponent<PlayerHiding>();
        }

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            speedHash = Animator.StringToHash(speedParam);
            lookHash = Animator.StringToHash(lookAroundParam);
            attackHash = Animator.StringToHash(attackParam);
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == speedHash) hasSpeedParam = true;
                if (p.nameHash == lookHash) hasLookParam = true;
                if (p.nameHash == attackHash) hasAttackParam = true;
            }
        }
    }

    void Start()
    {
        homePosition = transform.position;
        state = State.Patrol;
        GoToNextPatrolPoint();
    }

    void Update()
    {
        if (player == null) return;

        // player is dead -> stand down
        if (playerHealth != null && playerHealth.isDead)
        {
            if (state != State.Idle)
            {
                state = State.Idle;
                agent.isStopped = false;
                agent.ResetPath();
            }
            UpdateAnimator();
            return;
        }

        bool sees = CanSeePlayer();
        SeesPlayerNow = sees;
        if (sees)
        {
            lastSeenPosition = player.position;
            lastSeenTime = Time.time;
        }

        switch (state)
        {
            case State.Patrol:
                if (sees) { StartChase(); break; }

                if (waiting)
                {
                    waitTimer -= Time.deltaTime;
                    if (waitTimer <= 0f)
                    {
                        waiting = false;
                        GoToNextPatrolPoint();
                    }
                }
                else if (ReachedDestination() || StuckWhileMoving())
                {
                    waiting = true;
                    waitTimer = waitAtPatrolPoint;
                }
                break;

            case State.Chase:
                if (sees)
                {
                    // the player may be standing off the NavMesh, so aim for the nearest walkable point
                    agent.SetDestination(SnapToNavMesh(lastSeenPosition, 5f));

                    float dist = Vector3.Distance(transform.position, player.position);
                    if (dist <= attackRange && Time.time >= nextAttackTime)
                    {
                        StartAttack();
                    }
                    else
                    {
                        // can't get any closer? (player is somewhere it can't reach) -> give up after a while
                        bool notMoving = !agent.pathPending && agent.velocity.sqrMagnitude < 0.05f;
                        stuckTimer = (notMoving && dist > attackRange) ? stuckTimer + Time.deltaTime : 0f;
                        if (stuckTimer >= giveUpTime) GiveUpChase();
                    }
                }
                else if (Time.time - lastSeenTime < sightMemory)
                {
                    agent.SetDestination(SnapToNavMesh(lastSeenPosition, 5f));
                }
                else
                {
                    StartSearch();
                }
                break;

            case State.Attack:
                FacePlayer();
                attackTimer += Time.deltaTime;

                if (!hitDone && attackTimer >= attackHitDelay)
                {
                    hitDone = true;
                    TryHitPlayer();
                }

                if (attackTimer >= attackDuration)
                {
                    agent.isStopped = false;
                    nextAttackTime = Time.time + attackCooldown;
                    state = State.Chase;
                }
                break;

            case State.Search:
                if (sees) { StartChase(); break; }

                if (searchLooking)
                {
                    searchTimer -= Time.deltaTime;
                    if (searchTimer <= 0f)
                    {
                        searchLooking = false;
                        if (searchPointsLeft > 0)
                        {
                            // check another spot near where the player was last seen
                            searchPointsLeft--;
                            agent.SetDestination(RandomNavMeshPoint(lastSeenPosition, searchRadius));
                            moveStuckTimer = 0f;
                        }
                        else
                        {
                            ReturnToPatrol();
                        }
                    }
                }
                else if (ReachedDestination() || StuckWhileMoving())
                {
                    searchLooking = true;
                    searchTimer = lookAroundTime;
                    TryDiscoverHiddenPlayer();
                }
                break;
        }

        // pick speed by state
        if (state == State.Chase) agent.speed = chaseSpeed;
        else if (state == State.Search) agent.speed = searchSpeed;
        else agent.speed = patrolSpeed;

        UpdateAnimator();
    }

    // ---------- state changes ----------

    void StartChase()
    {
        stuckTimer = 0f;
        moveStuckTimer = 0f;
        state = State.Chase;
        waiting = false;
        searchLooking = false;
    }

    void StartSearch()
    {
        state = State.Search;
        searchLooking = false;
        searchPointsLeft = searchPointCount;
        moveStuckTimer = 0f;
        agent.SetDestination(SnapToNavMesh(lastSeenPosition, 5f));
    }

    // The player is visible but the monster can't get to them: stop standing there and search elsewhere
    void GiveUpChase()
    {
        stuckTimer = 0f;
        moveStuckTimer = 0f;
        blindUntil = Time.time + giveUpBlindTime;

        state = State.Search;
        searchLooking = false;
        searchPointsLeft = searchPointCount;
        agent.SetDestination(RandomNavMeshPoint(transform.position, searchRadius));
    }

    // true if the monster has been standing still for ~2 seconds while it should be walking somewhere
    bool StuckWhileMoving()
    {
        if (!agent.pathPending && agent.velocity.sqrMagnitude < 0.05f) moveStuckTimer += Time.deltaTime;
        else moveStuckTimer = 0f;
        return moveStuckTimer > 2f;
    }

    void ReturnToPatrol()
    {
        state = State.Patrol;
        waiting = false;
        searchLooking = false;
        GoToNextPatrolPoint();
    }

    void GoToNextPatrolPoint()
    {
        Vector3 target;
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            target = patrolPoints[patrolIndex] != null ? patrolPoints[patrolIndex].position : homePosition;
        }
        else
        {
            target = RandomNavMeshPoint(homePosition, wanderRadius);
        }
        moveStuckTimer = 0f;
        agent.SetDestination(SnapToNavMesh(target));
    }

    // ---------- attack ----------

    void StartAttack()
    {
        state = State.Attack;
        attackTimer = 0f;
        hitDone = false;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        OnAttackStart?.Invoke();

        if (animator != null && hasAttackParam)
            animator.SetTrigger(attackHash);
    }

    void TryHitPlayer()
    {
        // can't hit a player who slipped under the bed mid-swing
        if (hiding != null && hiding.IsHidden) return;

        // only hurts if the player is still within reach when the swing lands
        if (Vector3.Distance(transform.position, player.position) > hitRange) return;

        OnAttackHit?.Invoke();

        if (playerHealth != null)
            playerHealth.TakeDamage(attackDamage);
        else
            Debug.Log("Monster hit the player (add a PlayerHealth component to the Player)");
    }

    // While looking around, a monster that ends up close to your hiding place may find you
    void TryDiscoverHiddenPlayer()
    {
        if (hiding == null || !hiding.IsHidden) return;
        if (Vector3.Distance(transform.position, player.position) > hideDiscoverDistance) return;
        if (Random.value > hideDiscoverChance) return;

        hiding.ForceExit();                 // drag the player out
        lastSeenPosition = player.position;
        lastSeenTime = Time.time;
        StartChase();
    }

    void FacePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
    }

    // ---------- helpers ----------

    bool ReachedDestination()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f;
    }

    Vector3 SnapToNavMesh(Vector3 p, float maxDistance = 3f)
    {
        return NavMesh.SamplePosition(p, out NavMeshHit hit, maxDistance, NavMesh.AllAreas) ? hit.position : p;
    }

    Vector3 RandomNavMeshPoint(Vector3 center, float radius)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 c = Random.insideUnitCircle * radius;
            Vector3 p = center + new Vector3(c.x, 0f, c.y);
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                return hit.position;
        }
        // nothing walkable near "center": use a spot near the monster instead of standing still
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit own, 5f, NavMesh.AllAreas))
            return own.position;
        return transform.position;
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        if (hasSpeedParam)
            animator.SetFloat(speedHash, agent.velocity.magnitude, 0.1f, Time.deltaTime);

        if (hasLookParam)
            animator.SetBool(lookHash, state == State.Search && searchLooking);
    }

    bool CanSeePlayer()
    {
        // a hidden player can't be seen
        if (hiding != null && hiding.IsHidden) return false;

        // just gave up on an unreachable player: look elsewhere for a while
        if (Time.time < blindUntil) return false;

        // aim at the middle of the player's body, not their feet
        Vector3 target = playerCollider != null ? playerCollider.bounds.center : player.position + Vector3.up;
        Vector3 toTarget = target - eyes.position;
        float distance = toTarget.magnitude;

        if (distance > sightRange) return false;

        // inside the vision cone? (skipped when very close, or while already chasing/attacking)
        bool ignoreCone = distance <= closeAwarenessRadius
                          || (chaseSeesAllAround && (state == State.Chase || state == State.Attack));
        if (!ignoreCone)
        {
            float angle = Vector3.Angle(eyes.forward, toTarget);
            if (angle > fieldOfView * 0.5f) return false;
        }

        // nothing in the way?
        if (Physics.Raycast(eyes.position, toTarget.normalized, out RaycastHit hit, sightRange,
                            obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == player || hit.transform.IsChildOf(player);
        }
        return false;
    }

    // Scene view helpers (select the monster to see them)
    void OnDrawGizmosSelected()
    {
        Transform e = eyes != null ? eyes : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(e.position, sightRange);

        Vector3 left = Quaternion.Euler(0, -fieldOfView * 0.5f, 0) * e.forward;
        Vector3 right = Quaternion.Euler(0, fieldOfView * 0.5f, 0) * e.forward;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(e.position, left * sightRange);
        Gizmos.DrawRay(e.position, right * sightRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(e.position, closeAwarenessRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.green;
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            foreach (var p in patrolPoints)
                if (p != null) Gizmos.DrawSphere(p.position, 0.3f);
        }
        else
        {
            Gizmos.DrawWireSphere(Application.isPlaying ? homePosition : transform.position, wanderRadius);
        }
    }
}