using UnityEngine;
using UnityEngine.AI;

// Put this on the door LEAF (the part that swings), not on the frame or the wall.
// The door needs a Collider so the player can look at it (add a Box Collider if it has none).
// Create an empty child called "Hinge" on the hinge edge of the door and drag it into "hinge".
// PlayerInteract (on the Player) opens it with E, and the monster opens it automatically when it walks up.
public class InteractiveDoor : MonoBehaviour
{
    [Header("Hinge")]
    [Tooltip("Empty object on the hinge edge. If empty, the door turns around its own pivot.")]
    public Transform hinge;

    [Header("Opening")]
    public float openAngle = 95f;
    [Tooltip("Swing away from whoever opens it (so it never hits them)")]
    public bool swingAwayFromOpener = true;
    public float smoothTime = 0.25f;     // lower = faster swing
    public bool locked;
    [Tooltip("Optional: a NavMesh Obstacle (Carve ON) in the doorway. It blocks the monster while the door is locked.")]
    public NavMeshObstacle lockObstacle;

    [Header("Monster")]
    public bool openForMonster = true;
    public float monsterOpenDistance = 2.2f;
    public bool closeAfterMonsterPasses = false;

    [Header("Sounds (optional)")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    public bool IsOpen { get; private set; }

    Vector3 startPos, hingePos, doorCenter;
    Quaternion startRot;
    float angle, angleVelocity, targetAngle;
    bool openedByMonster;
    MonsterAI[] monsters;
    AudioSource audioSrc;

    void Awake()
    {
        startPos = transform.position;
        startRot = transform.rotation;
        hingePos = hinge != null ? hinge.position : transform.position;

        // centre of the closed door (used to work out which way to swing)
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            doorCenter = b.center;
        }
        else
        {
            doorCenter = transform.position;
        }

        monsters = FindObjectsByType<MonsterAI>(FindObjectsSortMode.None);

        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 1f;
        audioSrc.minDistance = 2f;
        audioSrc.maxDistance = 15f;

        if (lockObstacle != null) lockObstacle.enabled = locked;
    }

    void Update()
    {
        if (openForMonster) HandleMonsters();

        if (Mathf.Abs(angle - targetAngle) > 0.01f || Mathf.Abs(angleVelocity) > 0.01f)
        {
            angle = Mathf.SmoothDamp(angle, targetAngle, ref angleVelocity, smoothTime);
            Quaternion delta = Quaternion.AngleAxis(angle, Vector3.up);
            transform.position = hingePos + delta * (startPos - hingePos);
            transform.rotation = delta * startRot;
        }
    }

    // ---- called by PlayerInteract ----
    // returns false if the door is locked
    public bool Toggle(Transform opener)
    {
        if (IsOpen)
        {
            Close();
            return true;
        }
        if (locked)
        {
            Play(lockedSound);
            return false;
        }
        Open(opener.position);
        return true;
    }

    public void Open(Vector3 openerPosition)
    {
        if (IsOpen) return;
        IsOpen = true;

        if (swingAwayFromOpener)
        {
            // which side of the door is the opener on? swing the other way
            Vector3 toFreeEdge = doorCenter - hingePos; toFreeEdge.y = 0f;
            float side = Vector3.Dot(Vector3.Cross(Vector3.up, toFreeEdge), openerPosition - hingePos);
            targetAngle = (side >= 0f ? -1f : 1f) * Mathf.Abs(openAngle);
        }
        else
        {
            targetAngle = openAngle;
        }
        Play(openSound);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        openedByMonster = false;
        targetAngle = 0f;
        Play(closeSound);
    }

    public void Unlock()
    {
        locked = false;
        if (lockObstacle != null) lockObstacle.enabled = false;
    }

    public void Lock()
    {
        locked = true;
        if (lockObstacle != null) lockObstacle.enabled = true;
    }

    // ---- monster opens doors by walking up to them ----
    void HandleMonsters()
    {
        if (monsters == null) return;

        foreach (MonsterAI m in monsters)
        {
            if (m == null) continue;
            float dist = Vector3.Distance(m.transform.position, doorCenter);

            if (!IsOpen && !locked && dist <= monsterOpenDistance)
            {
                Open(m.transform.position);
                openedByMonster = true;
            }
            else if (IsOpen && openedByMonster && closeAfterMonsterPasses && dist > monsterOpenDistance + 2f)
            {
                Close();
            }
        }
    }

    void Play(AudioClip clip)
    {
        if (clip != null) audioSrc.PlayOneShot(clip, soundVolume);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 h = hinge != null ? hinge.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(h, 0.12f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Application.isPlaying ? doorCenter : transform.position, monsterOpenDistance);
    }
}
