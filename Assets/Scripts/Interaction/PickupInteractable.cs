using UnityEngine;
using UnityEngine.Events;

public class PickupInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "E - Pick up";
    [SerializeField] private GameObject objectToHide;
    [SerializeField] private bool destroyAfterPickup = true;
    [SerializeField] private UnityEvent onPickup;

    private bool pickedUp;

    public bool CanInteract(PlayerInteractor interactor)
    {
        return !pickedUp;
    }

    public string GetInteractionPrompt(PlayerInteractor interactor)
    {
        return pickedUp ? string.Empty : prompt;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (pickedUp)
        {
            return;
        }

        pickedUp = true;
        onPickup?.Invoke();

        GameObject target = objectToHide != null ? objectToHide : gameObject;

        if (destroyAfterPickup)
        {
            Destroy(target);
            return;
        }

        target.SetActive(false);
    }
}
