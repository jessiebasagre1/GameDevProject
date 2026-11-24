using UnityEngine;

// Put this on the Player cylinder (needs a CharacterController).
// Drag the child Camera into "cameraTransform".
// Hold Left Ctrl (or C) = crouch, Left Shift = sprint.
[RequireComponent(typeof(CharacterController))]
public class FPSPlayer : MonoBehaviour
{
    public Transform cameraTransform;

    [Header("Movement")]
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 5.5f;
    public float mouseSensitivity = 2f;
    public float gravity = -20f;

    [Header("Crouch")]
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode crouchKeyAlt = KeyCode.C;   // Ctrl can clash with Editor shortcuts, so C works too
    [Tooltip("Tick: press once to crouch and again to stand. Untick: hold to crouch.")]
    public bool toggleCrouch = false;
    public float crouchHeight = 1.2f;          // capsule height while crouching (standing height is read from the Character Controller)
    public float crouchSpeed = 1.8f;
    public float crouchTransitionSpeed = 6f;   // metres per second the height changes (higher = faster)

    // other scripts (like PlayerHiding and GameMenus) switch these on and off
    [HideInInspector] public bool lookEnabled = true;
    [HideInInspector] public bool movementEnabled = true;

    CharacterController controller;
    float xRotation;
    float yVelocity;

    // limited look (used while hiding)
    bool limitLook;
    float baseYaw, yawOffset, yawLimit, pitchLimit;

    // crouch state
    float standHeight, currentHeight, standCameraY;
    Vector3 standCenter;
    bool wantCrouch, crouching;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        standHeight = controller.height;
        standCenter = controller.center;
        currentHeight = standHeight;
        crouchHeight = Mathf.Max(crouchHeight, controller.radius * 2f + 0.01f);   // can't be shorter than the capsule is wide
        if (cameraTransform != null) standCameraY = cameraTransform.localPosition.y;
    }

    void Update()
    {
        if (lookEnabled) Look();
        if (movementEnabled) Move();
    }

    void Look()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (limitLook)
        {
            yawOffset = Mathf.Clamp(yawOffset + mx, -yawLimit, yawLimit);
            transform.rotation = Quaternion.Euler(0f, baseYaw + yawOffset, 0f);
            xRotation = Mathf.Clamp(xRotation - my, -pitchLimit, pitchLimit);
        }
        else
        {
            xRotation = Mathf.Clamp(xRotation - my, -85f, 85f);
            transform.Rotate(Vector3.up * mx);
        }

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void Move()
    {
        if (!controller.enabled) return;

        HandleCrouch();

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        bool sprinting = Input.GetKey(KeyCode.LeftShift) && !crouching;
        float speed = crouching ? crouchSpeed : (sprinting ? sprintSpeed : walkSpeed);

        Vector3 move = (transform.right * x + transform.forward * z);
        if (move.sqrMagnitude > 1f) move.Normalize();

        // stay on the ground, and fall when there is no ground
        if (controller.isGrounded && yVelocity < 0f) yVelocity = -2f;
        yVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * speed + Vector3.up * yVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    // ------------------------------------------------------------------ crouch

    void HandleCrouch()
    {
        bool pressed = Input.GetKeyDown(crouchKey) || Input.GetKeyDown(crouchKeyAlt);
        bool held = Input.GetKey(crouchKey) || Input.GetKey(crouchKeyAlt);

        if (toggleCrouch)
        {
            if (pressed) wantCrouch = !wantCrouch;
        }
        else
        {
            wantCrouch = held;
        }

        // stay crouched if there is a low ceiling above (you can't stand up into it)
        crouching = wantCrouch || (currentHeight < standHeight - 0.01f && !HasHeadroom());

        float target = crouching ? crouchHeight : standHeight;
        if (!Mathf.Approximately(currentHeight, target))
        {
            currentHeight = Mathf.MoveTowards(currentHeight, target, crouchTransitionSpeed * Time.deltaTime);
            ApplyShape();
        }
    }

    bool HasHeadroom()
    {
        float extra = standHeight - currentHeight;
        if (extra <= 0.01f) return true;

        Vector3 top = transform.TransformPoint(controller.center) + Vector3.up * (currentHeight / 2f - controller.radius);
        return !Physics.SphereCast(top, controller.radius * 0.9f, Vector3.up, out RaycastHit hit,
                                   extra + 0.05f, ~0, QueryTriggerInteraction.Ignore);
    }

    // keeps the feet on the floor while the capsule gets shorter, and lowers the camera with it
    void ApplyShape()
    {
        controller.height = currentHeight;
        controller.center = new Vector3(standCenter.x,
                                        standCenter.y - (standHeight - currentHeight) / 2f,
                                        standCenter.z);

        if (cameraTransform != null)
        {
            Vector3 p = cameraTransform.localPosition;
            p.y = standCameraY - (standHeight - currentHeight);
            cameraTransform.localPosition = p;
        }
    }

    // used by PlayerHiding: get up instantly before sliding under a bed
    public void ForceStand()
    {
        wantCrouch = false;
        crouching = false;
        currentHeight = standHeight;
        ApplyShape();
    }

    // ------------------------------------------------------------------ used by other scripts

    public void BeginLimitedLook(float yawLimitDegrees, float pitchLimitDegrees)
    {
        limitLook = true;
        baseYaw = transform.eulerAngles.y;
        yawOffset = 0f;
        yawLimit = yawLimitDegrees;
        pitchLimit = pitchLimitDegrees;
        xRotation = 0f;
    }

    public void EndLimitedLook()
    {
        limitLook = false;
    }

    public void ResetLook()
    {
        xRotation = 0f;
        yVelocity = 0f;
    }

    // used by the shotgun: tilts the view up a little
    public void AddPitchKick(float degrees)
    {
        xRotation = Mathf.Clamp(xRotation - degrees, -85f, 85f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}