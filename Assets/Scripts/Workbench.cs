using UnityEngine;
using UnityEngine.Events;

// Put this on a workbench (needs a Collider). Once the player has every blueprint, they hold E to build the shotgun.
// Building is LOUD: nearby monsters come to investigate while you work.
public class Workbench : MonoBehaviour
{
    public float buildTime = 5f;           // seconds the player must keep holding E
    public float noiseRadius = 30f;        // how far the building noise carries
    public float noiseInterval = 1.5f;     // how often the noise alerts monsters

    [Tooltip("Looped while building (hammering, drilling...)")]
    public AudioClip buildSound;

    public UnityEvent onShotgunBuilt;

    float progress;
    float noiseTimer;
    AudioSource loopSrc;

    public float Progress01 { get { return buildTime > 0f ? Mathf.Clamp01(progress / buildTime) : 1f; } }

    void Awake()
    {
        loopSrc = gameObject.AddComponent<AudioSource>();
        loopSrc.playOnAwake = false;
        loopSrc.loop = true;
        loopSrc.clip = buildSound;
        loopSrc.spatialBlend = 1f;
        loopSrc.minDistance = 3f;
        loopSrc.maxDistance = 25f;
    }

    public bool CanBuild(PlayerInventory inventory)
    {
        return !inventory.HasShotgun && inventory.HasAllBlueprints;
    }

    // called every frame while the player holds E on the bench
    public void Hold(PlayerInventory inventory, float deltaTime)
    {
        progress += deltaTime;
        if (loopSrc.clip != null && !loopSrc.isPlaying) loopSrc.Play();

        noiseTimer -= deltaTime;
        if (noiseTimer <= 0f)
        {
            noiseTimer = noiseInterval;
            MonsterAI.BroadcastNoise(transform.position, noiseRadius);
        }

        if (progress >= buildTime)
        {
            Release();
            inventory.GiveShotgun();
            onShotgunBuilt.Invoke();
        }
    }

    // called when the player lets go or looks away
    public void Release()
    {
        progress = 0f;
        noiseTimer = 0f;
        if (loopSrc.isPlaying) loopSrc.Stop();
    }
}
