using UnityEngine;

// Put this on the Player cylinder (next to FPSPlayer, PlayerHiding and PlayerInventory).
// Look at something and press E: open doors, pick up blueprints and shells, hold E at the workbench to build.
[RequireComponent(typeof(FPSPlayer))]
public class PlayerInteract : MonoBehaviour
{
    public Transform cameraTransform;
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 2.5f;

    PlayerHiding hiding;
    PlayerInventory inventory;

    InteractiveDoor door;
    WorldPickup pickup;
    Workbench bench;
    Workbench buildingBench;     // the bench being used right now

    string message;
    float messageTimer;
    GUIStyle style;

    void Awake()
    {
        hiding = GetComponent<PlayerHiding>();
        inventory = GetComponent<PlayerInventory>();
    }

    void Update()
    {
        if (messageTimer > 0f) messageTimer -= Time.deltaTime;

        door = null; pickup = null; bench = null;

        if (hiding != null && hiding.IsHidden)    // E is used to come out while hiding
        {
            StopBuilding();
            return;
        }

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit,
                            interactDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            door = hit.collider.GetComponentInParent<InteractiveDoor>();
            pickup = hit.collider.GetComponentInParent<WorldPickup>();
            bench = hit.collider.GetComponentInParent<Workbench>();
        }

        if (door != null && Input.GetKeyDown(interactKey))
        {
            if (!door.Toggle(transform))
            {
                message = "It's locked.";
                messageTimer = 1.5f;
            }
        }

        if (pickup != null && inventory != null && Input.GetKeyDown(interactKey))
            pickup.Collect(inventory);

        // workbench: keep holding the key to build
        if (bench != null && inventory != null && bench.CanBuild(inventory) && Input.GetKey(interactKey))
        {
            buildingBench = bench;
            bench.Hold(inventory, Time.deltaTime);
        }
        else
        {
            StopBuilding();
        }
    }

    void StopBuilding()
    {
        if (buildingBench != null)
        {
            buildingBench.Release();
            buildingBench = null;
        }
    }

    // simple on-screen prompts (no UI setup needed)
    void OnGUI()
    {
        string text = null;

        if (messageTimer > 0f) text = message;
        else if (door != null)
            text = "Press " + interactKey + (door.IsOpen ? " to close" : " to open");
        else if (pickup != null)
            text = "Press " + interactKey + " to pick up " + pickup.itemName;
        else if (bench != null && inventory != null && !inventory.HasShotgun)
        {
            if (!inventory.HasAllBlueprints)
                text = "You need all " + inventory.blueprintsNeeded + " blueprints (" + inventory.Blueprints + "/" + inventory.blueprintsNeeded + ")";
            else
                text = buildingBench != null ? "Building..." : "Hold " + interactKey + " to build the shotgun";
        }

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 22;
            style.normal.textColor = Color.white;
        }

        float y = Screen.height * 0.62f;
        if (text != null) GUI.Label(new Rect(0f, y, Screen.width, 40f), text, style);

        // build progress bar
        if (buildingBench != null)
        {
            float w = 300f, h = 14f;
            Rect back = new Rect((Screen.width - w) / 2f, y + 45f, w, h);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(back, Texture2D.whiteTexture);
            GUI.color = new Color(0.8f, 0.1f, 0.1f, 0.95f);
            GUI.DrawTexture(new Rect(back.x, back.y, w * buildingBench.Progress01, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}