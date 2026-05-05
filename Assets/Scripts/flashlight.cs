using UnityEngine;
using UnityEngine.InputSystem;

public class flashlight : MonoBehaviour
{
    [Header("Battery")]
    public float MaxBattary;
    public float currentBattery;
    public bool IsActive;

    [Header("References")]
    public GameObject flashlightGO;

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string flashlightActionName = "Flashlight";

    private InputActionMap actionMap;
    private InputAction flashlightAction;
    private bool ownsActionMap;

    private void Awake()
    {
        if (playerInput == null)
        {
            playerInput = GetComponentInParent<PlayerInput>();
        }

        if (playerInput != null && playerInput.actions != null)
        {
            actionMap = playerInput.actions.FindActionMap(actionMapName, true);
            ownsActionMap = false;
        }
        else if (inputActions != null)
        {
            actionMap = inputActions.FindActionMap(actionMapName, true);
            ownsActionMap = true;
        }

        if (actionMap == null)
        {
            Debug.LogError($"[{nameof(flashlight)}] Action map '{actionMapName}' was not found.", this);
            enabled = false;
            return;
        }

        flashlightAction = actionMap.FindAction(flashlightActionName, true);

        if (currentBattery <= 0f && MaxBattary > 0f)
        {
            currentBattery = MaxBattary;
        }

        ApplyFlashlightState();
    }

    private void OnEnable()
    {
        if (ownsActionMap && actionMap != null)
        {
            actionMap.Enable();
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
        if (flashlightAction != null && flashlightAction.WasPressedThisFrame())
        {
            ToggleFlashlight();
        }

        if (IsActive && currentBattery > 0f)
        {
            currentBattery -= Time.deltaTime;
        }

        if (IsActive && currentBattery <= 0f)
        {
            currentBattery = 0f;
            IsActive = false;
            ApplyFlashlightState();
        }
    }

    private void ToggleFlashlight()
    {
        if (!IsActive && currentBattery <= 0f)
        {
            return;
        }

        IsActive = !IsActive;
        ApplyFlashlightState();
    }

    private void ApplyFlashlightState()
    {
        if (flashlightGO != null)
        {
            flashlightGO.SetActive(IsActive);
        }
    }
}
