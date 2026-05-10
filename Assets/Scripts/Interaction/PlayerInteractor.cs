using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HorrorPlayerController playerController;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private InteractionPromptUI promptUI;

    [Header("Input")]
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string interactActionName = "Interact";

    [Header("Interaction")]
    [SerializeField, Min(0.5f)] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    public Camera InteractionCamera => interactionCamera;

    private InputActionMap actionMap;
    private InputAction interactAction;
    private bool ownsActionMap;
    private IInteractable currentInteractable;

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = GetComponent<HorrorPlayerController>();
        }

        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        if (interactionCamera == null && playerController != null)
        {
            interactionCamera = playerController.PlayerCamera;
        }

        if (interactionCamera == null)
        {
            interactionCamera = GetComponentInChildren<Camera>();
        }

        if (rayOrigin == null && interactionCamera != null)
        {
            rayOrigin = interactionCamera.transform;
        }

        ResolveInput();
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

        UpdatePrompt(null);
    }

    private void Update()
    {
        RefreshCurrentInteractable();

        if (currentInteractable != null && interactAction != null && interactAction.WasPressedThisFrame())
        {
            currentInteractable.Interact(this);
            RefreshCurrentInteractable();
        }
    }

    private void ResolveInput()
    {
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
            Debug.LogError($"[{nameof(PlayerInteractor)}] Action map '{actionMapName}' was not found.", this);
            enabled = false;
            return;
        }

        interactAction = actionMap.FindAction(interactActionName, true);
    }

    private void RefreshCurrentInteractable()
    {
        IInteractable nextInteractable = null;

        if (rayOrigin != null && Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit hit, interactDistance, interactMask, triggerInteraction))
        {
            nextInteractable = FindInteractable(hit.collider);

            if (nextInteractable != null && !nextInteractable.CanInteract(this))
            {
                nextInteractable = null;
            }
        }

        currentInteractable = nextInteractable;
        UpdatePrompt(currentInteractable);
    }

    private IInteractable FindInteractable(Collider hitCollider)
    {
        MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>(false);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IInteractable interactable)
            {
                return interactable;
            }
        }

        return null;
    }

    private void UpdatePrompt(IInteractable interactable)
    {
        if (promptUI == null)
        {
            return;
        }

        if (interactable == null)
        {
            promptUI.Hide();
            return;
        }

        string prompt = interactable.GetInteractionPrompt(this);

        if (string.IsNullOrWhiteSpace(prompt))
        {
            promptUI.Hide();
            return;
        }

        promptUI.Show(prompt);
    }

    private void OnDrawGizmosSelected()
    {
        Transform source = rayOrigin != null ? rayOrigin : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(source.position, source.forward * interactDistance);
    }
}
