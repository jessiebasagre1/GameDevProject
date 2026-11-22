using UnityEngine;
using UnityEngine.AI;

public class NavMeshPathDebugger : MonoBehaviour
{
    public Transform player;

    private NavMeshAgent agent;
    private NavMeshPath path;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        path = new NavMeshPath();
    }

    void Update()
    {
        if (player == null || agent == null) return;

        bool playerOnNavMesh = NavMesh.SamplePosition(
            player.position, out NavMeshHit hit, 2f, NavMesh.AllAreas);

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("MONSTER IS NOT ON NAVMESH");
            return;
        }

        if (!playerOnNavMesh)
        {
            Debug.LogError("PLAYER IS NOT ON NAVMESH");
            return;
        }

        bool hasPath = agent.CalculatePath(hit.position, path);

        Debug.Log($"Path: {path.status} | Calculated: {hasPath}");
    }
}