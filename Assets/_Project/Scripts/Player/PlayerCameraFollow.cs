using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraFollow : MonoBehaviour
{
    [Header("Alvo")]
    public Transform target;

    [Header("Enquadramento")]
    public Vector3 offset = new Vector3(0f, 3f, -6f);
    public Vector3 lookAtOffset = new Vector3(0f, 1.2f, 0f);
    [Min(0f)] public float smoothTime = 0.15f;

    [Header("Mouse")]
    [Min(0f)] public float mouseSensitivity = 0.12f;
    public float minPitch = -30f;
    public float maxPitch = 70f;

    private Vector3 followVelocity;
    private float yaw;
    private float pitch;
    private float followDistance;
    private bool orbitInitialized;

    private void Start()
    {
        InitializeOrbit();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            SetCursorLock(false);
        }

        if (Cursor.lockState != CursorLockMode.Locked && mouse.leftButton.wasPressedThisFrame)
        {
            SetCursorLock(true);
            return;
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            yaw += mouseDelta.x * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - mouseDelta.y * mouseSensitivity, minPitch, maxPitch);
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        if (!orbitInitialized)
        {
            InitializeOrbit();
            if (!orbitInitialized)
            {
                return;
            }
        }

        Vector3 lookAtPosition = target.position + lookAtOffset;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = lookAtPosition - orbitRotation * Vector3.forward * followDistance;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref followVelocity,
            Mathf.Max(0.0001f, smoothTime));

        Vector3 lookDirection = lookAtPosition - transform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }
    }

    private void OnDisable()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            SetCursorLock(false);
        }
    }

    private void InitializeOrbit()
    {
        if (target == null)
        {
            return;
        }

        Vector3 lookAtPosition = target.position + lookAtOffset;
        Vector3 cameraOffset = transform.position - lookAtPosition;
        if (cameraOffset.sqrMagnitude > 0.0001f)
        {
            followDistance = cameraOffset.magnitude;
            Quaternion lookRotation = Quaternion.LookRotation(-cameraOffset, Vector3.up);
            Vector3 angles = lookRotation.eulerAngles;
            yaw = angles.y;
            pitch = NormalizeAngle(angles.x);
        }
        else
        {
            Vector3 initialOffset = offset - lookAtOffset;
            followDistance = Mathf.Max(0.1f, initialOffset.magnitude);
            yaw = target.eulerAngles.y;
            pitch = Mathf.Clamp(0f, minPitch, maxPitch);
        }

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        orbitInitialized = true;
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    private static void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
