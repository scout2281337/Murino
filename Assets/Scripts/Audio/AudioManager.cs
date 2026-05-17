using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;


    [Header("Music")]
    [SerializeField] private AudioSource musicSource;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private Transform listener;
    [SerializeField] private LayerMask audioOcclusionMask = ~0;

    [Header("Sounds")]
    [SerializeField] private SoundData[] sounds;

    private Dictionary<string, SoundData> soundDictionary;
    private float sfxVolumeMultiplier = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        soundDictionary = new Dictionary<string, SoundData>();

        foreach (SoundData sound in sounds)
        {
            if (!string.IsNullOrWhiteSpace(sound.id))
            {
                soundDictionary[sound.id] = sound;
            }
        }

        listener = ResolveListener();
    }

    public void PlaySFX(string id)
    {
        if (soundDictionary.TryGetValue(id, out SoundData sound))
        {
            sfxSource.pitch = sound.EffectivePitch;

            sfxSource.PlayOneShot(sound.clip, sound.EffectiveVolume * sfxVolumeMultiplier);
        }
    }

    public AudioSource PlaySFXAtPosition(string id, Vector3 position)
    {
        if (!soundDictionary.TryGetValue(id, out SoundData sound) || sound.clip == null)
        {
            return null;
        }

        GameObject soundObject = new GameObject($"SFX_{id}");
        soundObject.transform.position = position;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        ConfigureSource(source, sound);
        source.loop = false;
        source.Play();

        SmartAudioOcclusion occlusion = soundObject.AddComponent<SmartAudioOcclusion>();
        occlusion.Initialize(GetListener(), audioOcclusionMask);

        Destroy(soundObject, sound.clip.length / Mathf.Max(0.01f, Mathf.Abs(sound.EffectivePitch)) + 0.25f);
        return source;
    }

    public AudioSource PlayLoopingSFXAtPosition(string id, Transform parent)
    {
        if (!soundDictionary.TryGetValue(id, out SoundData sound) || sound.clip == null)
        {
            return null;
        }

        GameObject soundObject = new GameObject($"LoopingSFX_{id}");
        soundObject.transform.SetParent(parent);
        soundObject.transform.localPosition = Vector3.zero;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        ConfigureSource(source, sound);
        source.loop = true;
        source.Play();

        SmartAudioOcclusion occlusion = soundObject.AddComponent<SmartAudioOcclusion>();
        occlusion.Initialize(GetListener(), audioOcclusionMask);

        return source;
    }

    public bool PlaySFXOnSource(string id, AudioSource source, bool attachOcclusion = true)
    {
        if (source == null)
        {
            Debug.LogWarning($"[{nameof(AudioManager)}] Cannot play '{id}': AudioSource is missing.");
            return false;
        }

        if (!soundDictionary.TryGetValue(id, out SoundData sound))
        {
            Debug.LogWarning($"[{nameof(AudioManager)}] Cannot play '{id}' on '{source.name}': sound id was not found.");
            return false;
        }

        if (sound.clip == null)
        {
            Debug.LogWarning($"[{nameof(AudioManager)}] Cannot play '{id}' on '{source.name}': AudioClip is missing.");
            return false;
        }

        ConfigureSource(source, sound);
        source.loop = sound.loop;
        source.Play();

        if (attachOcclusion)
        {
            AttachOcclusion(source);
        }

        return true;
    }

    public SmartAudioOcclusion AttachOcclusion(AudioSource source)
    {
        if (source == null)
        {
            return null;
        }

        SmartAudioOcclusion occlusion = source.GetComponent<SmartAudioOcclusion>();

        if (occlusion == null)
        {
            occlusion = source.gameObject.AddComponent<SmartAudioOcclusion>();
        }

        occlusion.Initialize(GetListener(), audioOcclusionMask);
        occlusion.RefreshBaseVolume();
        return occlusion;
    }

    public void PlayMusic(string id)
    {
        if (soundDictionary.TryGetValue(id, out SoundData sound))
        {
            musicSource.clip = sound.clip;
            musicSource.volume = sound.EffectiveVolume;
            musicSource.pitch = sound.EffectivePitch;
            musicSource.loop = sound.loop;

            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void SetMusicVolume(float value)
    {
        musicSource.volume = value;
    }

    public void SetSFXVolume(float value)
    {
        sfxVolumeMultiplier = Mathf.Clamp01(value);
        sfxSource.volume = sfxVolumeMultiplier;
    }

    private void ConfigureSource(AudioSource source, SoundData sound)
    {
        source.clip = sound.clip;
        source.volume = sound.EffectiveVolume * sfxVolumeMultiplier;
        source.pitch = sound.EffectivePitch;
        source.spatialBlend = sound.spatial ? 1f : 0f;
        source.minDistance = sound.EffectiveMinDistance;
        source.maxDistance = sound.EffectiveMaxDistance;

        source.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    private Transform GetListener()
    {
        listener = ResolveListener();
        return listener;
    }

    private Transform ResolveListener()
    {
        if (listener != null)
        {
            return listener;
        }

        AudioListener audioListener = FindFirstObjectByType<AudioListener>();

        if (audioListener != null)
        {
            return audioListener.transform;
        }

        return Camera.main != null ? Camera.main.transform : null;
    }

}
