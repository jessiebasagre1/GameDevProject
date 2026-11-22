using UnityEngine;

// Put this on the Player cylinder (next to FPSPlayer and PlayerHiding).
// Look at a door and press E to open or close it.
[RequireComponent(typeof(FPSPlayer))]
public class PlayerInteract : MonoBehaviour
{
    public Transform cameraTransform;
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 2.5f;

    PlayerHiding hiding;
    InteractiveDoor lookedAtDoor;

    string message;
    float messageTimer;
    GUIStyle style;

    void Awake()
    {
        hiding = GetComponent<PlayerHiding>();
    }

    void Update()
    {
        if (messageTimer > 0f) messageTimer -= Time.deltaTime;

        lookedAtDoor = null;
        if (hiding != null && hiding.IsHidden) return;   // E is used to come out while hiding

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit,
                            interactDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            lookedAtDoor = hit.collider.GetComponentInParent<InteractiveDoor>();
        }

        if (lookedAtDoor != null && Input.GetKeyDown(interactKey))
        {
            if (!lookedAtDoor.Toggle(transform))
            {
                message = "It's locked.";
                messageTimer = 1.5f;
            }
        }
    }

    // simple on-screen prompt (no UI setup needed)
    void OnGUI()
    {
        string text = null;
        if (messageTimer > 0f) text = message;
        else if (lookedAtDoor != null)
            text = "Press " + interactKey + (lookedAtDoor.IsOpen ? " to close" : " to open");
        if (text == null) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 22;
            style.normal.textColor = Color.white;
        }
        GUI.Label(new Rect(0f, Screen.height * 0.62f, Screen.width, 40f), text, style);
    }
}
