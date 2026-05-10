using UnityEngine;
using UnityEngine.Events;

public class EventInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "E - Interact";
    [SerializeField] private bool singleUse;
    [SerializeField, Min(0f)] private float cooldown = 0.15f;
    [SerializeField] private UnityEvent onInteract;
    [SerializeField] private UnityEvent onFirstInteract;

    private bool hasInteracted;
    private float lastInteractTime = float.NegativeInfinity;

    public bool CanInteract(PlayerInteractor interactor)
    {
        if (singleUse && hasInteracted)
        {
            return false;
        }

        return Time.time >= lastInteractTime + cooldown;
    }

    public string GetInteractionPrompt(PlayerInteractor interactor)
    {
        return CanInteract(interactor) ? prompt : string.Empty;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        if (!hasInteracted)
        {
            onFirstInteract?.Invoke();
        }

        hasInteracted = true;
        lastInteractTime = Time.time;
        onInteract?.Invoke();
    }
}
