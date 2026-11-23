using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Put this on a "Shotgun" object that is a child of the player's Camera (so it follows where you look).
// Under it: the gun model (remove its colliders!) and optionally a Point Light called MuzzleLight.
// Leave the Shotgun object disabled in the Hierarchy: it is switched on when the shotgun is built.
public class ShotgunWeapon : MonoBehaviour
{
    [Header("References (found automatically if left empty)")]
    public Transform cameraTransform;
    public PlayerInventory inventory;
    public FPSPlayer player;
    public PlayerHiding hiding;
    public Light muzzleLight;
    [Tooltip("Animator on the Shotgun_Rig model (triggers: Draw, Fire, DryFire, Reload). Found automatically.")]
    public Animator animator;

    [Header("Shooting")]
    public int pellets = 8;
    public float spreadAngle = 4f;          // degrees; bigger = wider spread
    public float pelletDamage = 15f;        // per pellet at close range
    public float range = 40f;
    public float falloffStart = 6f;         // full damage up to here
    public float falloffEnd = 25f;          // weakest from here on
    [Range(0f, 1f)] public float minDamageMultiplier = 0.25f;
    public float fireInterval = 0.9f;       // pump time between shots
    public LayerMask hitMask = ~0;
    public float noiseRadius = 45f;         // a gunshot alerts monsters this far away
    [Tooltip("Writes what the shot hit to the Console and draws red rays in the Scene view")]
    public bool debugLog = true;

    [Header("Ammo")]
    public int magazineSize = 2;
    public float reloadTime = 2.2f;
    public int Loaded { get; private set; }

    [Header("Feel")]
    public float weaponKick = 0.12f;        // how far the gun jumps back
    public float cameraKick = 2f;           // degrees the view tilts up
    public float muzzleFlashTime = 0.06f;

    [Header("Sounds (optional)")]
    public AudioClip fireSound;
    public AudioClip emptySound;
    public AudioClip reloadSound;

    AudioSource audioSrc;
    PlayerHealth health;
    Vector3 basePos;
    Quaternion baseRot;
    float kick, dip, nextFireTime, flashTimer;
    bool reloading;

    void Awake()
    {
        basePos = transform.localPosition;
        baseRot = transform.localRotation;

        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;

        if (player == null) player = GetComponentInParent<FPSPlayer>();
        if (inventory == null) inventory = GetComponentInParent<PlayerInventory>();
        if (hiding == null) hiding = GetComponentInParent<PlayerHiding>();
        health = GetComponentInParent<PlayerHealth>();
        if (cameraTransform == null && player != null) cameraTransform = player.cameraTransform;
        if (animator == null) animator = GetComponentInChildren<Animator>(true);

        if (muzzleLight != null) muzzleLight.enabled = false;
    }

    void OnEnable()
    {
        reloading = false;
    }

    // called by PlayerInventory when the shotgun is built: it starts loaded
    public void OnBuilt()
    {
        Loaded = magazineSize;
        Trigger("Draw");
        reloading = false;
        kick = 0f;
    }

    void Update()
    {
        // gun movement (kick back, dip while reloading)
        kick = Mathf.Lerp(kick, 0f, Time.deltaTime * 12f);
        dip = Mathf.Lerp(dip, (reloading && animator == null) ? 0.18f : 0f, Time.deltaTime * 8f);
        transform.localPosition = basePos - Vector3.forward * kick - Vector3.up * dip;
        transform.localRotation = baseRot * Quaternion.Euler(-kick * 80f + dip * 60f, 0f, 0f);

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && muzzleLight != null) muzzleLight.enabled = false;
        }

        if (reloading) return;
        if (Time.timeScale == 0f) return;
        if (hiding != null && hiding.IsHidden) return;
        if (health != null && health.isDead) return;

        if (Input.GetMouseButtonDown(0)) TryFire();
        if (Input.GetKeyDown(KeyCode.R)) StartReload();
    }

    void TryFire()
    {
        if (Time.time < nextFireTime) return;

        if (Loaded <= 0)
        {
            if (inventory != null && inventory.Shells > 0) StartReload();
            else
            {
                Play(emptySound);
                Trigger("DryFire");
                nextFireTime = Time.time + 0.4f;
            }
            return;
        }
        Fire();
    }

    void Fire()
    {
        Loaded--;
        nextFireTime = Time.time + fireInterval;

        Play(fireSound);
        Trigger("Fire");
        kick = (animator != null) ? 0f : weaponKick;     // the Fire animation does the kick
        if (player != null) player.AddPitchKick(cameraKick);
        if (muzzleLight != null)
        {
            muzzleLight.enabled = true;
            flashTimer = muzzleFlashTime;
        }

        // the blast is loud: monsters nearby come to investigate
        MonsterAI.BroadcastNoise(cameraTransform.position, noiseRadius);

        // fire all pellets, add up the damage per monster, then apply it once per shot
        Dictionary<MonsterHealth, float> damage = new Dictionary<MonsterHealth, float>();
        for (int i = 0; i < pellets; i++)
        {
            Vector3 dir = SpreadDirection();
            if (debugLog) Debug.DrawRay(cameraTransform.position, dir * range, Color.red, 1f);

            bool didHit = Physics.Raycast(cameraTransform.position, dir, out RaycastHit hit, range,
                                          hitMask, QueryTriggerInteraction.Ignore);
            if (debugLog && i == 0) LogHit(didHit, hit);

            if (didHit)
            {
                MonsterHealth target = hit.collider.GetComponentInParent<MonsterHealth>();
                if (target == null) continue;

                float t = Mathf.InverseLerp(falloffStart, falloffEnd, hit.distance);
                float amount = pelletDamage * Mathf.Lerp(1f, minDamageMultiplier, t);

                float current;
                damage.TryGetValue(target, out current);
                damage[target] = current + amount;
            }
        }

        foreach (KeyValuePair<MonsterHealth, float> kv in damage)
            kv.Key.TakeDamage(kv.Value, cameraTransform.position);
    }

    void LogHit(bool didHit, RaycastHit hit)
    {
        if (!didHit)
        {
            Debug.Log("Shotgun: the centre pellet hit nothing (out of range, or its layer is excluded by Hit Mask).");
            return;
        }
        MonsterHealth mh = hit.collider.GetComponentInParent<MonsterHealth>();
        Debug.Log("Shotgun: centre pellet hit '" + hit.collider.name + "' (layer "
                  + LayerMask.LayerToName(hit.collider.gameObject.layer) + ", " + hit.distance.ToString("0.0") + " m) -> "
                  + (mh != null ? "MonsterHealth FOUND" : "NO MonsterHealth on this object or its parents"));
    }

    Vector3 SpreadDirection()
    {
        Vector3 forward = cameraTransform.forward;
        float angle = Mathf.Sqrt(Random.value) * spreadAngle;
        Vector3 axis = Quaternion.AngleAxis(Random.Range(0f, 360f), forward) * cameraTransform.up;
        return Quaternion.AngleAxis(angle, axis) * forward;
    }

    void StartReload()
    {
        if (reloading || Loaded >= magazineSize) return;
        if (inventory == null || inventory.Shells <= 0) return;
        StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        reloading = true;
        Play(reloadSound);
        Trigger("Reload");
        yield return new WaitForSeconds(reloadTime);

        Loaded += inventory.TakeShells(magazineSize - Loaded);
        reloading = false;
    }

    void Play(AudioClip clip)
    {
        if (clip != null) audioSrc.PlayOneShot(clip);
    }

    void Trigger(string triggerName)
    {
        if (animator != null) animator.SetTrigger(triggerName);
    }
}