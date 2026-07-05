using UnityEngine;

/// <summary>
/// PlayerPrefs 기반 게임 설정 (볼륨 등).
/// </summary>
public static class GameSettings
{
    public const string SfxVolumeKey = "BeatDefender.SfxVolume";
    public const string MetronomeVolumeKey = "BeatDefender.MetronomeVolume";
    public const string MapLayoutKey = "BeatDefender.MapLayout";

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxVolumeKey, 0.5f);
        set
        {
            PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
            OnSfxVolumeChanged?.Invoke(SfxVolume);
        }
    }

    public static float MetronomeVolume
    {
        get => PlayerPrefs.GetFloat(MetronomeVolumeKey, 0.35f);
        set
        {
            PlayerPrefs.SetFloat(MetronomeVolumeKey, Mathf.Clamp01(value));
            OnMetronomeVolumeChanged?.Invoke(MetronomeVolume);
        }
    }

    public static MapLayoutKind SelectedMapLayout
    {
        get => (MapLayoutKind)PlayerPrefs.GetInt(MapLayoutKey, (int)MapLayoutKind.Classic);
        set => PlayerPrefs.SetInt(MapLayoutKey, (int)value);
    }

    public static string ActiveGameSceneName => SceneNames.GetGameScene(SelectedMapLayout);

    public static event System.Action<float> OnSfxVolumeChanged;
    public static event System.Action<float> OnMetronomeVolumeChanged;

    public static void ApplyTo(SimpleAudio audio)
    {
        if (audio == null)
            return;

        audio.sfxVolume = SfxVolume;
        audio.metronomeVolume = MetronomeVolume;
    }
}
