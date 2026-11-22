using UnityEngine;
using UnityEngine.AI;

// Put this on the Mutant Monster 7 root, next to MonsterAI. Drag your sound clips into the slots.
// It creates its own 3D Audio Sources, so you don't need to add any.
[RequireComponent(typeof(MonsterAI))]
[RequireComponent(typeof(NavMeshAgent))]
public class MonsterAudio : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip spotRoar;          // when it first sees you
    public AudioClip chaseLoop;         // loops while chasing / attacking
    public AudioClip idleGrowl;         // now and then while patrolling / searching
    public AudioClip breathingLoop;     // loops while patrolling / searching
    public AudioClip[] footsteps;       // random one is played per step
    public AudioClip attackSwing;       // when an attack starts
    public AudioClip attackHit;         // when the hit lands on the player

    [Header("Volumes")]
    [Range(0f, 1f)] public float roarVolume = 1f;
    [Range(0f, 1f)] public float chaseVolume = 0.8f;
    [Range(0f, 1f)] public float idleGrowlVolume = 0.6f;
    [Range(0f, 1f)] public float breathingVolume = 0.5f;
    [Range(0f, 1f)] public float footstepVolume = 0.8f;
    [Range(0f, 1f)] public float attackVolume = 1f;

    [Header("3D sound")]
    public float minDistance = 3f;      // full volume inside this distance
    public float maxDistance = 25f;     // silent beyond this distance

    [Header("Timing")]
    public float roarCooldown = 6f;               // stops the roar from repeating when it re-spots you quickly
    public float idleGrowlMinInterval = 8f;
    public float idleGrowlMaxInterval = 18f;
    public float loopFadeTime = 0.6f;

    [Header("Footsteps (played by distance walked)")]
    public float walkStride = 1.4f;               // metres per step when walking
    public float runStride = 2.2f;                // metres per step when running
    public float runSpeedThreshold = 3f;          // above this speed it counts as running
    public float footstepPitchVariation = 0.08f;

    MonsterAI ai;
    NavMeshAgent agent;
    AudioSource sfx, stepSrc, chaseSrc, breathSrc;
    MonsterAI.State lastState;
    float nextIdleGrowl;
    float lastRoarTime = -999f;
    float stepAccum;

    void Awake()
    {
        ai = GetComponent<MonsterAI>();
        agent = GetComponent<NavMeshAgent>();

        sfx = CreateSource(false, null);
        stepSrc = CreateSource(false, null);
        chaseSrc = CreateSource(true, chaseLoop);
        breathSrc = CreateSource(true, breathingLoop);

        lastState = ai.state;
        ScheduleIdleGrowl();
    }

    void OnEnable()
    {
        ai.OnAttackStart += HandleAttackStart;
        ai.OnAttackHit += HandleAttackHit;
    }

    void OnDisable()
    {
        ai.OnAttackStart -= HandleAttackStart;
        ai.OnAttackHit -= HandleAttackHit;
    }

    AudioSource CreateSource(bool loop, AudioClip clip)
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.clip = clip;
        s.volume = loop ? 0f : 1f;
        s.spatialBlend = 1f;                         // fully 3D
        s.rolloffMode = AudioRolloffMode.Logarithmic;
        s.minDistance = minDistance;
        s.maxDistance = maxDistance;
        return s;
    }

    void Update()
    {
        MonsterAI.State state = ai.state;
        bool chasing = state == MonsterAI.State.Chase || state == MonsterAI.State.Attack;

        // just spotted the player -> roar
        bool wasChasing = lastState == MonsterAI.State.Chase || lastState == MonsterAI.State.Attack;
        if (chasing && !wasChasing && Time.time - lastRoarTime > roarCooldown)
        {
            lastRoarTime = Time.time;
            if (spotRoar != null) sfx.PlayOneShot(spotRoar, roarVolume);
        }
        lastState = state;

        // loops fade in and out depending on what the monster is doing
        FadeLoop(chaseSrc, chasing ? chaseVolume : 0f);
        FadeLoop(breathSrc, (!chasing && state != MonsterAI.State.Idle) ? breathingVolume : 0f);

        // occasional growl while it hunts around
        bool roaming = state == MonsterAI.State.Patrol || state == MonsterAI.State.Search;
        if (roaming && Time.time >= nextIdleGrowl)
        {
            if (idleGrowl != null) sfx.PlayOneShot(idleGrowl, idleGrowlVolume);
            ScheduleIdleGrowl();
        }

        UpdateFootsteps();
    }

    void FadeLoop(AudioSource src, float target)
    {
        if (src.clip == null) return;

        src.volume = Mathf.MoveTowards(src.volume, target, Time.deltaTime / Mathf.Max(0.01f, loopFadeTime));

        if (src.volume > 0f && !src.isPlaying) src.Play();
        else if (src.volume <= 0f && src.isPlaying) src.Stop();
    }

    void UpdateFootsteps()
    {
        if (footsteps == null || footsteps.Length == 0) return;

        float speed = agent.velocity.magnitude;
        if (speed < 0.1f) return;

        stepAccum += speed * Time.deltaTime;
        float stride = speed > runSpeedThreshold ? runStride : walkStride;

        if (stepAccum >= stride)
        {
            stepAccum = 0f;
            AudioClip clip = footsteps[Random.Range(0, footsteps.Length)];
            if (clip == null) return;

            stepSrc.pitch = 1f + Random.Range(-footstepPitchVariation, footstepPitchVariation);
            stepSrc.PlayOneShot(clip, footstepVolume);
        }
    }

    void ScheduleIdleGrowl()
    {
        nextIdleGrowl = Time.time + Random.Range(idleGrowlMinInterval, idleGrowlMaxInterval);
    }

    // called by MonsterAI
    void HandleAttackStart()
    {
        if (attackSwing != null) sfx.PlayOneShot(attackSwing, attackVolume);
    }

    void HandleAttackHit()
    {
        if (attackHit != null) sfx.PlayOneShot(attackHit, attackVolume);
    }
}
