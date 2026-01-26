using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// ScriptableObject chứa tất cả AudioClip trong game.
/// Sử dụng Odin Inspector để hiển thị dạng Dictionary dễ quản lý.
/// </summary>
[CreateAssetMenu(fileName = "AudioData", menuName = "AG10/Audio Data", order = 0)]
public class AudioData : ScriptableObject
{
    [Serializable]
    public class SoundEntry
    {
        [HorizontalGroup("Sound")]
        [LabelWidth(100)]
        [EnumToggleButtons]
        public SoundType Type;

        [HorizontalGroup("Sound")]
        [ListDrawerSettings(ShowIndexLabels = false, DraggableItems = true, NumberOfItemsPerPage = 3)]
        [Required]
        [Tooltip("Playlist: Có thể có nhiều bài nhạc (tự động chuyển bài)")]
        public List<AudioClip> Clips = new List<AudioClip>();

        [HorizontalGroup("Sound")]
        [Range(0f, 1f)]
        [LabelText("Volume")]
        public float Volume = 1f;

        [HorizontalGroup("Sound")]
        [Tooltip("Phát ngẫu nhiên hay tuần tự?")]
        public bool Randomize = false;
    }

    [Title("Audio Configuration", "Quản lý tất cả âm thanh trong game", TitleAlignment = TitleAlignments.Centered)]
    [Space(10)]

    [FoldoutGroup("Music Tracks")]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
    [SerializeField] private List<SoundEntry> _musicTracks = new List<SoundEntry>();

    [FoldoutGroup("SFX Sounds")]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
    [SerializeField] private List<SoundEntry> _sfxSounds = new List<SoundEntry>();

    // Cache Dictionary để lookup nhanh
    private Dictionary<SoundType, SoundEntry> _soundCache;

    /// <summary>
    /// Khởi tạo cache khi load ScriptableObject
    /// </summary>
    private void OnEnable()
    {
        BuildCache();
    }

    /// <summary>
    /// Build cache từ Lists
    /// </summary>
    [Button(ButtonSizes.Medium), PropertyOrder(-1)]
    [GUIColor(0.3f, 0.8f, 0.3f)]
    private void BuildCache()
    {
        _soundCache = new Dictionary<SoundType, SoundEntry>();

        foreach (var entry in _musicTracks)
        {
            if (entry.Type != SoundType.None && entry.Clips != null && entry.Clips.Count > 0)
            {
                _soundCache[entry.Type] = entry;
            }
        }

        foreach (var entry in _sfxSounds)
        {
            if (entry.Type != SoundType.None && entry.Clips != null && entry.Clips.Count > 0)
            {
                _soundCache[entry.Type] = entry;
            }
        }

        Debug.Log($"[AudioData] Đã build cache với {_soundCache.Count} sounds.");
    }

    /// <summary>
    /// Lấy SoundEntry theo SoundType (để truy cập playlist)
    /// </summary>
    public SoundEntry GetEntry(SoundType type)
    {
        if (_soundCache == null || _soundCache.Count == 0)
        {
            BuildCache();
        }

        if (_soundCache.TryGetValue(type, out SoundEntry entry))
        {
            return entry;
        }

        // Không log warning ở đây để tránh spam console
        // AudioManager sẽ tự xử lý khi entry == null
        return null;
    }

    /// <summary>
    /// Lấy AudioClip theo SoundType (lấy clip đầu tiên trong playlist)
    /// </summary>
    public AudioClip GetClip(SoundType type)
    {
        SoundEntry entry = GetEntry(type);
        if (entry != null && entry.Clips.Count > 0)
        {
            return entry.Clips[0];
        }

        // Không log warning ở đây nữa
        return null;
    }

    /// <summary>
    /// Lấy Volume của sound
    /// </summary>
    public float GetVolume(SoundType type)
    {
        if (_soundCache == null || _soundCache.Count == 0)
        {
            BuildCache();
        }

        if (_soundCache.TryGetValue(type, out SoundEntry entry))
        {
            return entry.Volume;
        }

        return 1f;
    }

#if UNITY_EDITOR
    [Button(ButtonSizes.Large), PropertyOrder(-2)]
    [GUIColor(0.4f, 0.8f, 1f)]
    private void AutoPopulateFromResources()
    {
        Debug.Log("[AudioData] Auto-populate chưa được implement. Hãy add AudioClip thủ công.");
    }
#endif
}
