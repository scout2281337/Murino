using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform hinge;
    [SerializeField] private bool startsOpen;
    [SerializeField] private bool locked;
    [SerializeField] private string lockedPrompt = "Locked";
    [SerializeField] private string openPrompt = "E - Open";
    [SerializeField] private string closePrompt = "E - Close";
    [SerializeField, Min(1f)] private float openAngle = 90f;
    [SerializeField, Min(1f)] private float rotationSpeed = 180f;

    private Quaternion closedRotation;
    private Quaternion targetRotation;
    private bool isOpen;

    private void Awake()
    {
        if (hinge == null)
        {
            hinge = transform;
        }

        closedRotation = hinge.localRotation;
        isOpen = startsOpen;
        targetRotation = startsOpen ? GetOpenRotation(1f) : closedRotation;
        hinge.localRotation = targetRotation;
    }

    private void Update()
    {
        hinge.localRotation = Quaternion.RotateTowards(hinge.localRotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public string GetInteractionPrompt(PlayerInteractor interactor)
    {
        if (locked)
        {
            return lockedPrompt;
        }

        return isOpen ? closePrompt : openPrompt;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (locked)
        {
            return;
        }

        isOpen = !isOpen;

        if (!isOpen)
        {
            targetRotation = closedRotation;
            return;
        }

        float direction = 1f;

        if (interactor != null)
        {
            Vector3 toPlayer = interactor.transform.position - hinge.position;
            direction = Vector3.Dot(hinge.right, toPlayer) >= 0f ? -1f : 1f;
        }

        targetRotation = GetOpenRotation(direction);
    }

    public void SetLocked(bool value)
    {
        locked = value;
    }

    private Quaternion GetOpenRotation(float direction)
    {
        return closedRotation * Quaternion.Euler(0f, openAngle * direction, 0f);
    }
}
