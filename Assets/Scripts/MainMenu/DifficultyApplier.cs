using System;
using System.Linq;
using UnityEngine;

// Put this on an empty object called "Difficulty" in the GAME scene (not the menu scene).
// Drag every monster in the scene into "Monsters". The difficulty picked in the main menu decides how many
// of them are switched on (the first N) and what stats they get.
[DefaultExecutionOrder(-200)]      // runs before the monsters wake up, so their stats are already set
public class DifficultyApplier : MonoBehaviour
{
    [Serializable]
    public class Level
    {
        [Tooltip("How many monsters are active (the first N in the Monsters list)")]
        public int monsterCount = 1;

        [Header("Monster stats")]
        public int damage = 34;                 // damage per hit on the player
        public float maxHealth = 300f;
        public float patrolSpeed = 1.5f;        // walking around, not hunting
        public float searchSpeed = 2.5f;        // checking where it last saw / heard you
        public float chaseSpeed = 2.5f;         // running after you
        public float sightRange = 15f;
        public float attackCooldown = 0.5f;     // pause between attacks (smaller = more dangerous)

        public Level() { }

        public Level(int count, int damage, float health, float patrol, float search, float chase, float sight, float cooldown)
        {
            monsterCount = count; this.damage = damage; maxHealth = health;
            patrolSpeed = patrol; searchSpeed = search; chaseSpeed = chase;
            sightRange = sight; attackCooldown = cooldown;
        }
    }

    [Header("Monsters in this scene (the first N are used, the rest are switched off)")]
    public MonsterAI[] monsters;

    [Header("Difficulty settings")]
    public Level easy      = new Level(1, 20, 200f, 1.2f, 1.8f, 2.2f, 12f, 1.0f);
    public Level normal    = new Level(2, 34, 300f, 1.5f, 2.5f, 2.8f, 15f, 0.5f);
    public Level difficult = new Level(3, 50, 400f, 1.8f, 3.0f, 3.4f, 18f, 0.3f);

    [Header("Testing")]
    [Tooltip("Editor only: ignore the main menu choice and use the difficulty below")]
    public bool useTestDifficulty;
    public Difficulty testDifficulty = Difficulty.Normal;

    void Awake()
    {
        Difficulty d = (useTestDifficulty && Application.isEditor) ? testDifficulty : GameSettings.Selected;
        Level level = d == Difficulty.Easy ? easy : (d == Difficulty.Normal ? normal : difficult);

        // if the list was left empty, use every monster in the scene
        if (monsters == null || monsters.Length == 0)
        {
            monsters = FindObjectsByType<MonsterAI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                       .OrderBy(m => m.name).ToArray();
        }

        if (level.monsterCount > monsters.Length)
        {
            Debug.LogWarning("DifficultyApplier: " + d + " wants " + level.monsterCount + " monsters but only "
                             + monsters.Length + " are in the Monsters list. Duplicate a monster and add it to the list.");
        }

        int active = 0;
        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterAI m = monsters[i];
            if (m == null) continue;

            bool on = i < level.monsterCount;
            m.gameObject.SetActive(on);
            if (!on) continue;

            Apply(m, level);
            active++;
        }

        Debug.Log("Difficulty: " + d + " - " + active + " monster(s) active");
    }

    static void Apply(MonsterAI m, Level level)
    {
        m.attackDamage = level.damage;
        m.patrolSpeed = level.patrolSpeed;
        m.searchSpeed = level.searchSpeed;
        m.chaseSpeed = level.chaseSpeed;
        m.sightRange = level.sightRange;
        m.attackCooldown = level.attackCooldown;

        MonsterHealth health = m.GetComponent<MonsterHealth>();
        if (health != null) health.maxHealth = level.maxHealth;
    }
}
