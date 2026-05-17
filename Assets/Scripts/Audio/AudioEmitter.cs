using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioEmitter : MonoBehaviour
{
    [SerializeField] private string soundId;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool attachOcclusion = true;
    [SerializeField] private bool stopOnDisable = true;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        if (stopOnDisable && audioSource != null)
        {
            audioSource.Stop();
        }
    }

    public void Play()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning($"[{nameof(AudioEmitter)}] AudioManager was not found.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(soundId))
        {
            Debug.LogWarning($"[{nameof(AudioEmitter)}] Sound id is empty.", this);
            return;
        }

        if (!AudioManager.Instance.PlaySFXOnSource(soundId, audioSource, attachOcclusion))
        {
            Debug.LogWarning($"[{nameof(AudioEmitter)}] Failed to play '{soundId}'.", this);
        }
    }

    public void Stop()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}
