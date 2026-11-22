using UnityEngine;
using UnityEngine.SceneManagement;

// Put this on the Player cylinder (next to FPSPlayer).
public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public float restartDelay = 3f;

    public int CurrentHealth { get; private set; }
    public bool isDead { get; private set; }

    void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        CurrentHealth -= amount;
        Debug.Log("Player health: " + CurrentHealth);
        // TODO: red screen flash, hurt sound, camera shake

        if (CurrentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;

        var movement = GetComponent<FPSPlayer>();
        if (movement != null) movement.enabled = false;

        // TODO: death screen / jumpscare
        Invoke(nameof(Restart), restartDelay);
    }

    void Restart()
    {
        // the scene must be added in File > Build Profiles (or Build Settings)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
