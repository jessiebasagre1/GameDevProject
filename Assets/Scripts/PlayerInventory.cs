using UnityEngine;

// Put this on the Player cylinder. Tracks blueprints and shells, builds the shotgun, and draws a simple HUD.
public class PlayerInventory : MonoBehaviour
{
    [Header("Blueprints")]
    public int blueprintsNeeded = 4;

    [Header("Shotgun")]
    [Tooltip("The Shotgun object under the camera. Leave it UNCHECKED (disabled) in the Hierarchy until it is built.")]
    public ShotgunWeapon shotgun;
    public int startingShells = 2;

    [Header("Crosshair (appears once the shotgun is built)")]
    public bool showCrosshair = true;
    public Color crosshairColor = new Color(1f, 1f, 1f, 0.9f);
    public float crosshairSize = 10f;         // length of each tick, in pixels
    public float crosshairThickness = 2f;
    public float crosshairGap = 6f;           // empty space around the centre
    public float crosshairKick = 10f;         // the gap opens this much after a shot
    [Tooltip("Optional: use your own crosshair image instead of the lines")]
    public Texture2D crosshairTexture;
    public float crosshairTextureSize = 48f;

    public int Blueprints { get; private set; }
    public int Shells { get; private set; }
    public bool HasShotgun { get; private set; }
    public bool HasAllBlueprints { get { return Blueprints >= blueprintsNeeded; } }

    MonsterHealth monster;
    PlayerHiding hiding;
    PlayerHealth health;
    string message;
    float messageTimer;
    GUIStyle bigStyle, smallStyle;

    void Start()
    {
        Shells = startingShells;
        monster = FindFirstObjectByType<MonsterHealth>();
        hiding = GetComponent<PlayerHiding>();
        health = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (messageTimer > 0f) messageTimer -= Time.deltaTime;
    }

    public void AddBlueprint(int amount)
    {
        Blueprints = Mathf.Min(Blueprints + amount, blueprintsNeeded);

        if (HasAllBlueprints) ShowMessage("All blueprints found! Build the shotgun at the workbench.");
        else ShowMessage("Shotgun blueprint found (" + Blueprints + "/" + blueprintsNeeded + ")");
    }

    public void AddShells(int amount)
    {
        Shells += amount;
        ShowMessage("+" + amount + " shotgun shell" + (amount == 1 ? "" : "s"));
    }

    // returns how many shells were actually taken
    public int TakeShells(int amount)
    {
        int taken = Mathf.Min(amount, Shells);
        Shells -= taken;
        return taken;
    }

    public void GiveShotgun()
    {
        HasShotgun = true;
        if (shotgun != null)
        {
            shotgun.gameObject.SetActive(true);
            shotgun.OnBuilt();
        }
        ShowMessage("Shotgun built! Left click to shoot, R to reload.", 4f);
    }

    public void ShowMessage(string text, float seconds = 3f)
    {
        message = text;
        messageTimer = seconds;
    }

    void OnGUI()
    {
        if (bigStyle == null)
        {
            bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            bigStyle.normal.textColor = Color.white;
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            smallStyle.normal.textColor = new Color(1f, 1f, 1f, 0.85f);
        }

        // objective (top-left)
        string objective;
        if (!HasShotgun)
            objective = HasAllBlueprints ? "Build the shotgun at the workbench"
                                         : "Find the blueprints (" + Blueprints + "/" + blueprintsNeeded + ")";
        else if (monster != null && monster.IsDead)
            objective = "The monster is dead";
        else
            objective = "Destroy the monster";

        GUI.Label(new Rect(20f, 16f, 600f, 30f), objective, smallStyle);

        // ammo (bottom-right)
        if (HasShotgun && shotgun != null)
        {
            GUIStyle ammo = new GUIStyle(bigStyle) { alignment = TextAnchor.MiddleRight };
            GUI.Label(new Rect(Screen.width - 220f, Screen.height - 60f, 200f, 40f),
                      shotgun.Loaded + " / " + Shells, ammo);
        }

        // popup message (top-centre)
        if (messageTimer > 0f)
            GUI.Label(new Rect(0f, Screen.height * 0.12f, Screen.width, 40f), message, bigStyle);

        DrawCrosshair();
    }

    // ---------- crosshair ----------

    void DrawCrosshair()
    {
        if (!showCrosshair || !HasShotgun || shotgun == null) return;
        if (hiding != null && hiding.IsHidden) return;
        if (health != null && health.isDead) return;

        Color c = crosshairColor;
        if (shotgun.IsReloading) c.a *= 0.35f;                 // dimmed while reloading

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        if (crosshairTexture != null)
        {
            float s = crosshairTextureSize;
            GUI.color = c;
            GUI.DrawTexture(new Rect(cx - s / 2f, cy - s / 2f, s, s), crosshairTexture);
            GUI.color = Color.white;
            return;
        }

        // the gap opens for a moment after every shot, then closes again
        float kick = Mathf.Clamp01(1f - shotgun.TimeSinceLastShot / 0.25f) * crosshairKick;
        float gap = crosshairGap + kick;
        float t = crosshairThickness;
        float len = crosshairSize;

        DrawBar(cx - gap - len, cy - t / 2f, len, t, c);       // left
        DrawBar(cx + gap, cy - t / 2f, len, t, c);             // right
        DrawBar(cx - t / 2f, cy - gap - len, t, len, c);       // top
        DrawBar(cx - t / 2f, cy + gap, t, len, c);             // bottom
        DrawBar(cx - t / 2f, cy - t / 2f, t, t, c);            // centre dot
    }

    void DrawBar(float x, float y, float w, float h, Color c)
    {
        GUI.color = new Color(0f, 0f, 0f, c.a * 0.6f);         // thin dark outline so it shows on bright walls
        GUI.DrawTexture(new Rect(x - 1f, y - 1f, w + 2f, h + 2f), Texture2D.whiteTexture);
        GUI.color = c;
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}