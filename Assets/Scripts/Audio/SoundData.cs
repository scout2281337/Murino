using UnityEngine;
[System.Serializable]
public class SoundData
{
    private const float DefaultVolume = 1f;
    private const float DefaultPitch = 1f;
    private const float DefaultMinDistance = 1f;
    private const float DefaultMaxDistance = 18f;

    public string id;

    public AudioClip clip;

    [Range(0f, 1f)] 
    public float volume;

    [Range(0.1f, 3f)]
    public float pitch;

    public bool loop;

    [Header("3D Audio")]
    public bool spatial = true;

    [Min(0f)]
    public float minDistance = 1f;

    [Min(0.01f)]
    public float maxDistance = 18f;

    public float EffectiveVolume => volume <= 0f ? DefaultVolume : volume;
    public float EffectivePitch => pitch <= 0f ? DefaultPitch : pitch;
    public float EffectiveMinDistance => minDistance <= 0f ? DefaultMinDistance : minDistance;
    public float EffectiveMaxDistance => maxDistance <= EffectiveMinDistance ? DefaultMaxDistance : maxDistance;
}
