using UnityEngine;

// Put this on a blueprint or a box of shells lying in the level.
// It needs a normal (NOT trigger) Collider so the player can look at it. Press E to pick it up.
public class WorldPickup : MonoBehaviour
{
    public enum Kind { Blueprint, Shells }

    public Kind kind = Kind.Blueprint;
    public int amount = 1;
    public string itemName = "Shotgun Blueprint";   // shown in the prompt: "Press E to pick up ..."

    [Header("Sound (optional)")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    [Header("Look")]
    public bool spinAndBob = true;
    public float spinSpeed = 45f;
    public float bobHeight = 0.05f;

    [Header("Animation (optional)")]
    [Tooltip("Animator on the pickup model with a 'Collect' trigger. If one is found, the built-in spin/bob is switched off.")]
    public Animator animator;
    public float collectAnimationTime = 0.7f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) spinAndBob = false;     // the Float animation already spins and bobs
    }

    void Update()
    {
        if (!spinAndBob) return;
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * 2f) * bobHeight);
    }

    // called by PlayerInteract
    public void Collect(PlayerInventory inventory)
    {
        switch (kind)
        {
            case Kind.Blueprint: inventory.AddBlueprint(amount); break;
            case Kind.Shells: inventory.AddShells(amount); break;
        }

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, soundVolume);

        // switch off colliders so it can't be picked up twice, then play the collect animation (if there is one)
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;

        if (animator != null)
        {
            animator.SetTrigger("Collect");
            Destroy(gameObject, collectAnimationTime);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}