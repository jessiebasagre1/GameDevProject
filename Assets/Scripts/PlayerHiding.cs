using System.Collections;
using UnityEngine;

// Put this on the Player cylinder (next to FPSPlayer and PlayerHealth).
// Look at a HidingSpot and press E to hide. Press E again to come out.
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(FPSPlayer))]
public class PlayerHiding : MonoBehaviour
{
    public Transform cameraTransform;
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 2.5f;
    public float transitionTime = 0.6f;

    [Tooltip("Optional. Stops you from hiding while this monster is looking at you.")]
    public MonsterAI monster;

    public bool IsHidden { get; private set; }

    CharacterController controller;
    FPSPlayer fps;
    HidingSpot currentSpot, lookedAtSpot;
    Vector3 standCameraLocalPos;
    bool transitioning;

    string message;
    float messageTimer;
    GUIStyle style;

    float Half { get { return controller.height * 0.5f; } }
    Vector3 EntryPos(HidingSpot s) { return s.exitPoint.position + Vector3.up * Half; }
    Vector3 HidePos(HidingSpot s) { return s.hidePoint.position + Vector3.up * Half; }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        fps = GetComponent<FPSPlayer>();
    }

    void Update()
    {
        if (messageTimer > 0f) messageTimer -= Time.deltaTime;
        if (transitioning) return;

        if (IsHidden)
        {
            if (Input.GetKeyDown(interactKey)) StartCoroutine(ExitRoutine());
            return;
        }

        lookedAtSpot = FindSpot();
        if (lookedAtSpot != null && Input.GetKeyDown(interactKey))
            TryHide(lookedAtSpot);
    }

    HidingSpot FindSpot()
    {
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit,
                            interactDistance, ~0, QueryTriggerInteraction.Collide))
        {
            HidingSpot spot = hit.collider.GetComponentInParent<HidingSpot>();
            if (spot != null && !spot.IsOccupied && spot.hidePoint != null && spot.exitPoint != null)
                return spot;
        }
        return null;
    }

    void TryHide(HidingSpot spot)
    {
        if (monster != null && monster.SeesPlayerNow)
        {
            message = "It's watching you!";
            messageTimer = 1.5f;
            return;
        }
        StartCoroutine(HideRoutine(spot));
    }

    IEnumerator HideRoutine(HidingSpot spot)
    {
        transitioning = true;
        currentSpot = spot;
        spot.IsOccupied = true;

        fps.movementEnabled = false;
        fps.lookEnabled = false;
        controller.enabled = false;          // no collisions while sliding under the bed
        standCameraLocalPos = cameraTransform.localPosition;

        Vector3 lowCam = new Vector3(standCameraLocalPos.x, -Half + spot.cameraHeightAboveFloor, standCameraLocalPos.z);
        float yaw = spot.hidePoint.eulerAngles.y;

        yield return MoveTo(EntryPos(spot), yaw, standCameraLocalPos, 0.35f);   // walk to the entry spot
        yield return MoveTo(EntryPos(spot), yaw, lowCam, 0.3f);                 // get low
        yield return MoveTo(HidePos(spot), yaw, lowCam, transitionTime);        // slide under

        fps.ResetLook();
        fps.BeginLimitedLook(spot.lookYawLimit, spot.lookPitchLimit);
        fps.lookEnabled = true;
        IsHidden = true;
        transitioning = false;
    }

    IEnumerator ExitRoutine()
    {
        transitioning = true;
        fps.lookEnabled = false;
        fps.EndLimitedLook();

        HidingSpot spot = currentSpot;
        Vector3 lowCam = new Vector3(standCameraLocalPos.x, -Half + spot.cameraHeightAboveFloor, standCameraLocalPos.z);
        float yaw = spot.exitPoint.eulerAngles.y;

        yield return MoveTo(EntryPos(spot), yaw, lowCam, transitionTime);          // slide out
        yield return MoveTo(EntryPos(spot), yaw, standCameraLocalPos, 0.35f);      // stand up

        spot.IsOccupied = false;
        FinishExit();
    }

    // Called by the monster when it finds you: drags you out instantly.
    public void ForceExit()
    {
        if (!IsHidden && !transitioning) return;

        StopAllCoroutines();
        if (currentSpot != null)
        {
            transform.position = EntryPos(currentSpot);
            transform.rotation = Quaternion.Euler(0f, currentSpot.exitPoint.eulerAngles.y, 0f);
            currentSpot.IsOccupied = false;
        }
        cameraTransform.localPosition = standCameraLocalPos;
        cameraTransform.localRotation = Quaternion.identity;
        FinishExit();
    }

    void FinishExit()
    {
        fps.EndLimitedLook();
        fps.ResetLook();
        controller.enabled = true;
        fps.movementEnabled = true;
        fps.lookEnabled = true;
        IsHidden = false;
        transitioning = false;
        currentSpot = null;
    }

    IEnumerator MoveTo(Vector3 pos, float yaw, Vector3 camLocalPos, float duration)
    {
        Vector3 p0 = transform.position;
        float y0 = transform.eulerAngles.y;
        Vector3 c0 = cameraTransform.localPosition;
        Quaternion cr0 = cameraTransform.localRotation;

        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            float s = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(p0, pos, s);
            transform.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(y0, yaw, s), 0f);
            cameraTransform.localPosition = Vector3.Lerp(c0, camLocalPos, s);
            cameraTransform.localRotation = Quaternion.Slerp(cr0, Quaternion.identity, s);
            yield return null;
        }

        transform.position = pos;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraTransform.localPosition = camLocalPos;
        cameraTransform.localRotation = Quaternion.identity;
    }

    // simple on-screen prompt (no UI setup needed)
    void OnGUI()
    {
        string text = null;
        if (messageTimer > 0f) text = message;
        else if (!transitioning)
        {
            if (IsHidden) text = "Press " + interactKey + " to come out";
            else if (lookedAtSpot != null) text = "Press " + interactKey + " to hide";
        }
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
