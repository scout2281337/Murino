using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class HorrorPlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private bool lockCursorOnEnable = true;

    [Header("Input")]
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string sprintActionName = "Sprint";
    [SerializeField] private string crouchActionName = "Crouch";
    [SerializeField] private string jumpActionName = "Jump";

    [Header("Movement")]
    [SerializeField, Min(0f)] private float walkSpeed = 2.2f;
    [SerializeField, Min(0f)] private float sprintSpeed = 4.4f;
    [SerializeField, Min(0f)] private float crouchSpeed = 1.3f;
    [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundedForce = -2f;

    [Header("Look")]
    [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.12f;
    [SerializeField, Min(0.01f)] private float gamepadSensitivity = 165f;
    [SerializeField, Range(1f, 89f)] private float maxLookAngle = 80f;

    [Header("Head Bob")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private bool enableIdleSway = true;
    [SerializeField, Min(0f)] private float idleSwayFrequency = 0.9f;
    [SerializeField, Min(0f)] private float idleSwayHorizontalAmplitude = 0.012f;
    [SerializeField, Min(0f)] private float idleSwayVerticalAmplitude = 0.018f;
    [SerializeField, Min(0f)] private float walkBobFrequency = 1.8f;
    [SerializeField, Min(0f)] private float walkBobHorizontalAmplitude = 0.04f;
    [SerializeField, Min(0f)] private float walkBobVerticalAmplitude = 0.055f;
    [SerializeField, Min(0f)] private float sprintBobMultiplier = 1.8f;
    [SerializeField, Min(0f)] private float crouchBobMultiplier = 0.7f;
    [SerializeField, Min(0f)] private float bobSmoothing = 12f;

    [Header("Fear Camera Shake")]
    [SerializeField] private bool enableFearShake = true;
    [SerializeField, Min(0f)] private float fearShakeStartDistance = 14f;
    [SerializeField, Min(0f)] private float fearShakeMaxDistance = 3f;
    [SerializeField, Min(0f)] private float fearShakePositionAmplitude = 0.055f;
    [SerializeField, Min(0f)] private float fearShakeFrequency = 18f;
    [SerializeField, Min(0f)] private float chasingShakeMultiplier = 1.7f;
    [SerializeField, Min(0.01f)] private float fearShakeResponse = 7f;
    [SerializeField, Min(0.1f)] private float monsterScanInterval = 0.5f;

    [Header("Crouch")]
    [SerializeField, Min(0.5f)] private float crouchHeight = 1.05f;
    [SerializeField, Min(1f)] private float crouchTransitionSpeed = 10f;
    [SerializeField, Min(0f)] private float cameraCrouchOffset = 0.45f;
    [SerializeField, Min(0f)] private float ceilingCheckPadding = 0.05f;
    [SerializeField] private LayerMask standUpBlockMask = ~0;

    [Header("Stamina")]
    [SerializeField, Min(1f)] private float maxStamina = 5f;
    [SerializeField, Min(0f)] private float sprintDrainPerSecond = 1.1f;
    [SerializeField, Min(0f)] private float staminaRecoveryPerSecond = 0.85f;
    [SerializeField, Min(0f)] private float staminaRecoveryDelay = 1f;
    [SerializeField, Min(0f)] private float minStaminaToSprint = 0.35f;

    public Camera PlayerCamera => playerCamera;
    public Transform CameraRoot => cameraRoot;
    public float CurrentStamina => currentStamina;
    public float StaminaNormalized => maxStamina <= 0f ? 0f : currentStamina / maxStamina;
    public bool IsCrouching => isCrouching;
    public bool IsSprinting => isSprinting;
    public bool IsMoving => horizontalVelocity.sqrMagnitude > 0.01f;
    public float StealthVisibilityMultiplier => isCrouching ? 0.45f : 1f;
    public float NoiseMultiplier => GetNoiseMultiplier();
    public Vector3 HorizontalVelocity => horizontalVelocity;

    private CharacterController characterController;
    private InputActionMap actionMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private InputAction crouchAction;
    private InputAction jumpAction;
    private bool ownsActionMap;

    private float standingHeight;
    private Vector3 standingCenter;
    private Vector3 standingCameraLocalPosition;
    private Vector3 crouchingCameraLocalPosition;
    private float pitch;
    private float verticalVelocity;
    private float currentStamina;
    private float recoveryDelayTimer;
    private float bobTimer;
    private float idleSwayTimer;
    private float fearShakeTimer;
    private float fearShakeIntensity;
    private float nextMonsterScanTime;
    private Vector3 horizontalVelocity;
    private bool isCrouching;
    private bool isSprinting;
    private MonsterEnemyAI[] monsters = System.Array.Empty<MonsterEnemyAI>();
    private readonly Collider[] standUpHits = new Collider[8];

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (cameraRoot == null && playerCamera != null)
        {
            cameraRoot = playerCamera.transform;
        }

        if (cameraRoot == null)
        {
            cameraRoot = transform;
        }

        if (playerCamera == null && cameraRoot.TryGetComponent(out Camera cameraComponent))
        {
            playerCamera = cameraComponent;
        }

        ResolveInput();

        standingHeight = characterController.height;
        standingCenter = characterController.center;
        standingCameraLocalPosition = cameraRoot.localPosition;
        crouchingCameraLocalPosition = standingCameraLocalPosition + Vector3.down * cameraCrouchOffset;
        currentStamina = maxStamina;

        if (cameraRoot != null)
        {
            pitch = cameraRoot.localEulerAngles.x;

            if (pitch > 180f)
            {
                pitch -= 360f;
            }
        }
    }

    private void OnEnable()
    {
        if (ownsActionMap && actionMap != null)
        {
            actionMap.Enable();
        }

        if (lockCursorOnEnable)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        if (ownsActionMap && actionMap != null)
        {
            actionMap.Disable();
        }
    }

    private void Update()
    {
        if (crouchAction != null && crouchAction.WasPressedThisFrame())
        {
            ToggleCrouch();
        }

        HandleLook();
        HandleMovement();
        UpdateCrouchTransition();
        UpdateHeadBob();
        UpdateStamina();
    }

    private void ResolveInput()
    {
        if (playerInput != null && playerInput.actions != null)
        {
            actionMap = playerInput.actions.FindActionMap(actionMapName, true);
            ownsActionMap = false;

            if (playerInput.currentActionMap == null || playerInput.currentActionMap.name != actionMapName)
            {
                playerInput.SwitchCurrentActionMap(actionMapName);
            }
        }
        else if (inputActions != null)
        {
            actionMap = inputActions.FindActionMap(actionMapName, true);
            ownsActionMap = true;
        }

        if (actionMap == null)
        {
            Debug.LogError($"[{nameof(HorrorPlayerController)}] Action map '{actionMapName}' was not found.", this);
            enabled = false;
            return;
        }

        moveAction = actionMap.FindAction(moveActionName, true);
        lookAction = actionMap.FindAction(lookActionName, true);
        sprintAction = actionMap.FindAction(sprintActionName, true);
        crouchAction = actionMap.FindAction(crouchActionName, true);
        jumpAction = actionMap.FindAction(jumpActionName, true);
    }

    private void HandleLook()
    {
        if (lookAction == null || cameraRoot == null)
        {
            return;
        }

        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        float sensitivity = IsUsingGamepad() ? gamepadSensitivity * Time.deltaTime : mouseSensitivity;

        transform.Rotate(Vector3.up, lookInput.x * sensitivity);

        pitch -= lookInput.y * sensitivity;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
        cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 moveDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;

        bool hasMoveInput = moveInput.sqrMagnitude > 0.01f;
        bool wantsSprint = sprintAction != null && sprintAction.IsPressed();
        bool canSprint = wantsSprint && hasMoveInput && !isCrouching && currentStamina > 0f;
        bool hasRequiredStamina = currentStamina >= minStaminaToSprint || isSprinting;
        isSprinting = canSprint && hasRequiredStamina;

        float speed = walkSpeed;

        if (isCrouching)
        {
            speed = crouchSpeed;
        }
        else if (isSprinting)
        {
            speed = sprintSpeed;
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedForce;
        }

        if (jumpAction != null && jumpAction.WasPressedThisFrame() && characterController.isGrounded && !isCrouching)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = moveDirection * speed;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);

        horizontalVelocity = characterController.velocity;
        horizontalVelocity.y = 0f;
    }

    private void UpdateCrouchTransition()
    {
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float currentHeight = Mathf.Lerp(characterController.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        if (Mathf.Abs(currentHeight - targetHeight) < 0.01f)
        {
            currentHeight = targetHeight;
        }

        characterController.height = currentHeight;
        characterController.center = GetCenterForHeight(currentHeight);

        if (cameraRoot == null)
        {
            return;
        }

        Vector3 targetCameraPosition = isCrouching ? crouchingCameraLocalPosition : standingCameraLocalPosition;
        cameraRoot.localPosition = Vector3.Lerp(cameraRoot.localPosition, targetCameraPosition, crouchTransitionSpeed * Time.deltaTime);
    }

    private void UpdateHeadBob()
    {
        if (cameraRoot == null)
        {
            return;
        }

        Vector3 baseCameraPosition = isCrouching ? crouchingCameraLocalPosition : standingCameraLocalPosition;
        Vector3 horizontalVelocity = characterController.velocity;
        horizontalVelocity.y = 0f;

        bool isMovingOnGround = characterController.isGrounded && horizontalVelocity.sqrMagnitude > 0.01f;
        Vector3 targetOffset = Vector3.zero;

        if (enableHeadBob && isMovingOnGround)
        {
            float currentSpeed = horizontalVelocity.magnitude;
            float sprintBlend = sprintSpeed > walkSpeed
                ? Mathf.InverseLerp(walkSpeed, sprintSpeed, currentSpeed)
                : 0f;
            float amplitudeMultiplier = Mathf.Lerp(1f, sprintBobMultiplier, sprintBlend);
            float frequencyMultiplier = Mathf.Lerp(1f, 1.35f, sprintBlend);

            if (isCrouching)
            {
                amplitudeMultiplier *= crouchBobMultiplier;
                frequencyMultiplier *= 0.85f;
            }

            float frequency = walkBobFrequency * frequencyMultiplier;
            bobTimer += Time.deltaTime * frequency * Mathf.PI * 2f;
            idleSwayTimer = 0f;

            float horizontalOffset = Mathf.Cos(bobTimer * 0.5f) * walkBobHorizontalAmplitude * amplitudeMultiplier;
            float verticalOffset = Mathf.Abs(Mathf.Sin(bobTimer)) * walkBobVerticalAmplitude * amplitudeMultiplier;
            targetOffset = new Vector3(horizontalOffset, verticalOffset, 0f);
        }
        else if (enableHeadBob && enableIdleSway)
        {
            bobTimer = 0f;
            idleSwayTimer += Time.deltaTime * idleSwayFrequency * Mathf.PI * 2f;

            float crouchMultiplier = isCrouching ? 0.8f : 1f;
            float horizontalOffset = Mathf.Cos(idleSwayTimer * 0.65f) * idleSwayHorizontalAmplitude * crouchMultiplier;
            float verticalOffset = Mathf.Sin(idleSwayTimer) * idleSwayVerticalAmplitude * crouchMultiplier;
            targetOffset = new Vector3(horizontalOffset, verticalOffset, 0f);
        }
        else
        {
            bobTimer = 0f;
            idleSwayTimer = 0f;
        }

        targetOffset += GetFearShakeOffset();

        Vector3 targetCameraPosition = baseCameraPosition + targetOffset;
        cameraRoot.localPosition = Vector3.Lerp(cameraRoot.localPosition, targetCameraPosition, bobSmoothing * Time.deltaTime);
    }

    private void UpdateStamina()
    {
        if (isSprinting)
        {
            currentStamina = Mathf.Max(0f, currentStamina - sprintDrainPerSecond * Time.deltaTime);
            recoveryDelayTimer = staminaRecoveryDelay;

            if (currentStamina <= 0f)
            {
                isSprinting = false;
            }

            return;
        }

        if (recoveryDelayTimer > 0f)
        {
            recoveryDelayTimer -= Time.deltaTime;
            return;
        }

        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRecoveryPerSecond * Time.deltaTime);
    }

    private void ToggleCrouch()
    {
        if (isCrouching)
        {
            if (!CanStandUp())
            {
                return;
            }

            isCrouching = false;
            return;
        }

        isCrouching = true;
        isSprinting = false;
    }

    private bool CanStandUp()
    {
        float radius = Mathf.Max(0.05f, characterController.radius - characterController.skinWidth);
        Vector3 bottom = transform.position + standingCenter - Vector3.up * (standingHeight * 0.5f - radius);
        Vector3 top = transform.position + standingCenter + Vector3.up * (standingHeight * 0.5f - radius - ceilingCheckPadding);

        int hitCount = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, standUpHits, standUpBlockMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = standUpHits[i];

            if (hit == null)
            {
                continue;
            }

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private Vector3 GetCenterForHeight(float targetHeight)
    {
        float bottom = standingCenter.y - standingHeight * 0.5f;
        float centerY = bottom + targetHeight * 0.5f;
        return new Vector3(standingCenter.x, centerY, standingCenter.z);
    }

    private bool IsUsingGamepad()
    {
        return playerInput != null && playerInput.currentControlScheme == "Gamepad";
    }

    private Vector3 GetFearShakeOffset()
    {
        if (!enableFearShake)
        {
            fearShakeIntensity = Mathf.MoveTowards(fearShakeIntensity, 0f, fearShakeResponse * Time.deltaTime);
            return Vector3.zero;
        }

        float targetIntensity = GetNearbyMonsterThreat();
        fearShakeIntensity = Mathf.MoveTowards(fearShakeIntensity, targetIntensity, fearShakeResponse * Time.deltaTime);

        if (fearShakeIntensity <= 0.001f)
        {
            return Vector3.zero;
        }

        fearShakeTimer += Time.deltaTime * fearShakeFrequency;
        float shakeX = (Mathf.PerlinNoise(fearShakeTimer, 0.17f) - 0.5f) * 2f;
        float shakeY = (Mathf.PerlinNoise(0.43f, fearShakeTimer) - 0.5f) * 2f;

        return new Vector3(shakeX, shakeY, 0f) * fearShakePositionAmplitude * fearShakeIntensity;
    }

    private float GetNearbyMonsterThreat()
    {
        if (Time.time >= nextMonsterScanTime)
        {
            monsters = FindObjectsByType<MonsterEnemyAI>(FindObjectsSortMode.None);
            nextMonsterScanTime = Time.time + monsterScanInterval;
        }

        float strongestThreat = 0f;

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterEnemyAI monster = monsters[i];

            if (monster == null || !monster.isActiveAndEnabled)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, monster.transform.position);

            if (distance > fearShakeStartDistance)
            {
                continue;
            }

            float distanceThreat = Mathf.InverseLerp(fearShakeStartDistance, fearShakeMaxDistance, distance);
            float chaseMultiplier = monster.IsChasing ? chasingShakeMultiplier : 1f;
            strongestThreat = Mathf.Max(strongestThreat, distanceThreat * chaseMultiplier);
        }

        return Mathf.Clamp01(strongestThreat);
    }

    private float GetNoiseMultiplier()
    {
        if (!IsMoving)
        {
            return 0f;
        }

        if (isCrouching)
        {
            return 0.25f;
        }

        if (isSprinting)
        {
            return 1.75f;
        }

        return 1f;
    }
}
