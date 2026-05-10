using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text promptLabel;
    [SerializeField, Min(0.01f)] private float fadeSpeed = 10f;

    private float targetAlpha;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        HideInstant();
    }

    private void Update()
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        canvasGroup.blocksRaycasts = targetAlpha > 0.01f;
        canvasGroup.interactable = targetAlpha > 0.01f;
    }

    public void Show(string prompt)
    {
        if (promptLabel != null)
        {
            promptLabel.text = prompt;
        }

        targetAlpha = 1f;
    }

    public void Hide()
    {
        targetAlpha = 0f;
    }

    public void HideInstant()
    {
        targetAlpha = 0f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (promptLabel != null)
        {
            promptLabel.text = string.Empty;
        }
    }
}
