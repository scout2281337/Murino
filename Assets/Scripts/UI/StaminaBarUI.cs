using UnityEngine;
using UnityEngine.UI;

public class StaminaBarUI : MonoBehaviour
{
    [SerializeField] private HorrorPlayerController playerController;
    [SerializeField] private Image fillImage;
    [SerializeField, Min(0.01f)] private float lerpSpeed = 8f;
    [SerializeField] private bool hideWhenFull = true;
    [SerializeField] private CanvasGroup canvasGroup;

    private float currentFill = 1f;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void Update()
    {
        if (playerController == null || fillImage == null)
        {
            return;
        }

        currentFill = Mathf.Lerp(currentFill, playerController.StaminaNormalized, lerpSpeed * Time.deltaTime);
        fillImage.fillAmount = currentFill;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = hideWhenFull && currentFill >= 0.995f ? 0f : 1f;
        }
    }
}
