using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimento")]
    [Min(0f)] public float speed = 5f;
    [Min(0f)] public float jumpHeight = 1.2f;
    public float gravity = -20f;

    [Header("Referencias")]
    public Camera cam;
    public Animator animator;
    public InputActionAsset inputActions;

    private const float TurnSpeedDegrees = 720f;
    private const float AnimatorDampTime = 0.12f;
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");

    private CharacterController characterController;
    private InputAction moveAction;
    private InputAction jumpAction;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("PlayerMovement precisa de um CharacterController.", this);
            enabled = false;
            return;
        }

        InputActionAsset projectActions = InputSystem.actions;
        moveAction = projectActions != null ? projectActions.FindAction("Move", false) : null;
        jumpAction = projectActions != null ? projectActions.FindAction("Jump", false) : null;

        if (inputActions != null)
        {
            moveAction = moveAction ?? inputActions.FindAction("Move", false);
            jumpAction = jumpAction ?? inputActions.FindAction("Jump", false);
        }

        if (moveAction == null)
        {
            Debug.LogError("A acao Move nao foi encontrada no Input Actions.", this);
        }

        if (jumpAction == null)
        {
            Debug.LogError("A acao Jump nao foi encontrada no Input Actions.", this);
        }

        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("Camera.main nao encontrada; o movimento usara a orientacao do jogador.", this);
            }
        }
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        jumpAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
    }

    private void Update()
    {
        if (characterController == null)
        {
            return;
        }

        Vector2 input = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 moveDirection = GetCameraRelativeDirection(input);

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                TurnSpeedDegrees * Time.deltaTime);
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (characterController.isGrounded && jumpAction != null && jumpAction.WasPressedThisFrame())
        {
            verticalVelocity = Mathf.Sqrt(Mathf.Max(0f, jumpHeight * -2f * gravity));
        }

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 velocity = moveDirection * speed + Vector3.up * verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);

        if (animator != null)
        {
            float animationSpeed = moveDirection.sqrMagnitude > 0.0001f ? 1f : 0f;
            animator.SetFloat(SpeedParameter, animationSpeed, AnimatorDampTime, Time.deltaTime);
        }
    }

    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        Transform reference = cam != null ? cam.transform : transform;
        Vector3 forward = Vector3.ProjectOnPlane(reference.forward, Vector3.up);

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        }

        forward.Normalize();
        Vector3 right = Vector3.ProjectOnPlane(reference.right, Vector3.up);
        if (right.sqrMagnitude < 0.0001f)
        {
            right = Vector3.Cross(Vector3.up, forward);
        }

        right.Normalize();
        Vector3 direction = right * input.x + forward * input.y;
        return direction.sqrMagnitude > 1f ? direction.normalized : direction;
    }
}
