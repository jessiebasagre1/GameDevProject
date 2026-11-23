using UnityEngine;
using UnityEngine.Events;

// Put this on the monster root (next to MonsterAI). The monster also needs a Collider for shots to hit it.
[RequireComponent(typeof(MonsterAI))]
public class MonsterHealth : MonoBehaviour
{
    public float maxHealth = 300f;

    [Tooltip("A single shot dealing at least this much makes the monster flinch")]
    public float staggerDamage = 60f;
    [Tooltip("Minimum seconds between flinches, so it can't be stun-locked")]
    public float staggerCooldown = 4f;

    [Tooltip("Hook up anything you like: a win screen, unlocking an exit, loading a scene...")]
    public UnityEvent onDied;

    [Tooltip("Writes every hit to the Console")]
    public bool debugLog = true;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    MonsterAI ai;
    float nextStaggerTime;

    void Awake()
    {
        ai = GetComponent<MonsterAI>();
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount, Vector3 attackerPosition)
    {
        if (IsDead) return;

        CurrentHealth -= amount;
        if (debugLog) Debug.Log("Monster took " + amount.ToString("0") + " damage, health left: " + Mathf.Max(CurrentHealth, 0f).ToString("0"));
        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            ai.Die();
            onDied.Invoke();
            return;
        }

        bool stagger = amount >= staggerDamage && Time.time >= nextStaggerTime;
        if (stagger) nextStaggerTime = Time.time + staggerCooldown;

        ai.ReactToHit(attackerPosition, stagger);
    }
}