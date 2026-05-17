using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SmartAudioOcclusion : MonoBehaviour
{
    [SerializeField] private Transform listener;
    [SerializeField] private LayerMask occlusionMask = ~0;
    [SerializeField, Min(0f)] private float checkInterval = 0.08f;
    [SerializeField, Min(0f)] private float blockedVolumeMultiplier = 0.45f;
    [SerializeField, Range(500f, 22000f)] private float blockedLowPassCutoff = 1300f;
    [SerializeField, Range(500f, 22000f)] private float clearLowPassCutoff = 22000f;
    [SerializeField, Min(0.01f)] private float smoothing = 9f;
    [SerializeField] private bool enforceMaxDistance;
    [SerializeField, Range(0f, 1f)] private float minimumVolumeMultiplier = 0.08f;
    [SerializeField] private bool debugOcclusion;

    private AudioSource audioSource;
    private AudioLowPassFilter lowPassFilter;
    private float baseVolume;
    private float nextCheckTime;
    private float targetVolumeMultiplier = 1f;
    private float targetCutoff;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        lowPassFilter = GetComponent<AudioLowPassFilter>();

        if (lowPassFilter == null)
        {
            lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
        }

        baseVolume = audioSource.volume;
        targetCutoff = clearLowPassCutoff;
        lowPassFilter.cutoffFrequency = clearLowPassCutoff;

        if (listener == null && Camera.main != null)
        {
            listener = Camera.main.transform;
        }

        if (listener == null)
        {
            AudioListener audioListener = FindFirstObjectByType<AudioListener>();

            if (audioListener != null)
            {
                listener = audioListener.transform;
            }
        }
    }

    private void Update()
    {
        if (listener == null)
        {
            return;
        }

        if (Time.time >= nextCheckTime)
        {
            UpdateOcclusionTarget();
            nextCheckTime = Time.time + checkInterval;
        }

        float blend = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        float targetVolume = baseVolume * Mathf.Max(minimumVolumeMultiplier, targetVolumeMultiplier);
        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, blend);
        lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, targetCutoff, blend);
    }

    public void Initialize(Transform audioListener, LayerMask wallMask)
    {
        listener = audioListener;
        occlusionMask = wallMask;

        if (listener == null)
        {
            AudioListener sceneListener = FindFirstObjectByType<AudioListener>();

            if (sceneListener != null)
            {
                listener = sceneListener.transform;
            }
        }
    }

    public void RefreshBaseVolume()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        baseVolume = audioSource != null ? audioSource.volume : 1f;
    }

    private void UpdateOcclusionTarget()
    {
        Vector3 toListener = listener.position - transform.position;
        float distance = toListener.magnitude;
        float distanceVolumeMultiplier = GetHardDistanceMultiplier(distance);

        if (distance <= 0.01f)
        {
            targetVolumeMultiplier = distanceVolumeMultiplier;
            targetCutoff = clearLowPassCutoff;
            return;
        }

        int wallCount = CountWallsBetween(transform.position, toListener.normalized, distance);

        if (wallCount <= 0)
        {
            targetVolumeMultiplier = distanceVolumeMultiplier;
            targetCutoff = clearLowPassCutoff;
            LogDebug(distance, wallCount, targetVolumeMultiplier);
            return;
        }

        targetVolumeMultiplier = Mathf.Pow(blockedVolumeMultiplier, wallCount) * distanceVolumeMultiplier;
        targetCutoff = Mathf.Lerp(clearLowPassCutoff, blockedLowPassCutoff, Mathf.Clamp01(wallCount / 3f));
        LogDebug(distance, wallCount, targetVolumeMultiplier);
    }

    private float GetHardDistanceMultiplier(float distance)
    {
        if (!enforceMaxDistance || audioSource == null || audioSource.spatialBlend <= 0f)
        {
            return 1f;
        }

        float maxDistance = audioSource.maxDistance;

        if (maxDistance <= audioSource.minDistance)
        {
            return 1f;
        }

        if (distance >= maxDistance)
        {
            return 0f;
        }

        return 1f;
    }

    private int CountWallsBetween(Vector3 origin, Vector3 direction, float distance)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, occlusionMask, QueryTriggerInteraction.Ignore);
        int wallCount = 0;
        Transform listenerRoot = listener != null ? listener.root : null;

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;

            if (hitTransform == null)
            {
                continue;
            }

            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                continue;
            }

            if (listener != null && (hitTransform == listener || hitTransform.IsChildOf(listener)))
            {
                continue;
            }

            if (listenerRoot != null && (hitTransform == listenerRoot || hitTransform.IsChildOf(listenerRoot)))
            {
                continue;
            }

            wallCount++;
        }

        return wallCount;
    }

    private void LogDebug(float distance, int wallCount, float volumeMultiplier)
    {
        if (!debugOcclusion)
        {
            return;
        }

        Debug.Log(
            $"[{nameof(SmartAudioOcclusion)}] {name}: listener='{(listener != null ? listener.name : "null")}', distance={distance:F2}, max={audioSource.maxDistance:F2}, walls={wallCount}, multiplier={volumeMultiplier:F2}, volume={audioSource.volume:F2}.",
            this);
    }
}
