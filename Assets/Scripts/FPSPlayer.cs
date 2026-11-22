using UnityEngine;

// Put this on the Player cylinder (needs a CharacterController).
// Drag the child Camera into "cameraTransform".
[RequireComponent(typeof(CharacterController))]
public class FPSPlayer : MonoBehaviour
{
    public Transform cameraTransform;
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 5.5f;
    public float mouseSensitivity = 2f;
    public float gravity = -20f;

    // other scripts (like PlayerHiding) switch these on and off
    [HideInInspector] public bool lookEnabled = true;
    [HideInInspector] public bool movementEnabled = true;

    CharacterController controller;
    float xRotation;
    float yVelocity;

    // limited look (used while hiding)
    bool limitLook;
    float baseYaw, yawOffset, yawLimit, pitchLimit;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        Vector3 move = (transform.right * x + transform.forward * z);
        if (move.sqrMagnitude > 1f) move.Normalize();

        if (controller.isGrounded && yVelocity < 0f) yVelocity = -2f;
        yVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * speed + Vector3.up * yVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    // ---- used by PlayerHiding ----

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
}