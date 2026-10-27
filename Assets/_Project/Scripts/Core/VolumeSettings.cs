using System;
using UnityEngine;
using UnityEngine.Audio;

public static class VolumeSettings
{
    public const string BGMParam = "BGMVolume";
    public const string SFXParam = "SFXVolume";
    private const float DefaultVolume = 80f;

    public static float Load(string param)
    {
        return PlayerPrefs.GetFloat(param, DefaultVolume);
    }

    public static void Set(AudioMixer mixer, string param, float value)
    {
        PlayerPrefs.SetFloat(param, value);
        mixer.SetFloat(param, ToDecibel(value));
    }

    public static void ApplyAll(AudioMixer mixer)
    {
        Set(mixer, BGMParam, Load(BGMParam));
        Set(mixer, SFXParam, Load(SFXParam));
    }

    private static float ToDecibel(float value)
    {
        if (value <= 0f) return -80f;
        return Mathf.Log10(value / 100f) * 20f;
    }
}
