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

    public int Blueprints { get; private set; }
    public int Shells { get; private set; }
    public bool HasShotgun { get; private set; }
    public bool HasAllBlueprints { get { return Blueprints >= blueprintsNeeded; } }

    MonsterHealth monster;
    string message;
    float messageTimer;
    GUIStyle bigStyle, smallStyle;

    void Start()
    {
        Shells = startingShells;
        monster = FindFirstObjectByType<MonsterHealth>();
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
    }
}
