using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;

/// <summary>
/// Singleton Manager quản lý toàn bộ Audio trong game.
/// DontDestroyOnLoad để tồn tại xuyên suốt các Scene.
/// Hỗ trợ Playlist tự động chuyển bài.
/// Lưu Volume vào PlayerPrefs.
/// </summary>
public class AudioManager : MonoBehaviour
{
    #region Singleton Pattern

    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManager>();

                if (_instance == null)
                {
                    GameObject go = new GameObject("[AudioManager]");
                    _instance = go.AddComponent<AudioManager>();
                }
            }
            return _instance;
        }
    }

    #endregion

    #region Inspector Fields

    [Title("Configuration", TitleAlignment = TitleAlignments.Centered)]
    [Required]
    [SerializeField] private AudioData _audioData;

    [Space(10)]
    [Title("Audio Sources", TitleAlignment = TitleAlignments.Left)]
    [Required]
    [SerializeField] private AudioSource _musicSource;

    [Required]
    [SerializeField] private AudioSource _sfxSource;

    [Space(10)]
    [Title("Settings", TitleAlignment = TitleAlignments.Left)]
    [Range(0f, 1f)]
    [SerializeField] private float _defaultMusicVolume = 0.7f;

    [Range(0f, 1f)]
    [SerializeField] private float _defaultSFXVolume = 1f;

    [Space(10)]
    [Title("Transition", TitleAlignment = TitleAlignments.Left)]
    [Tooltip("Thời gian fade khi chuyển nhạc")]
    [Range(0f, 3f)]
    [SerializeField] private float _fadeDuration = 0.5f;

    #endregion

    #region PlayerPrefs Keys

    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    #endregion

    #region Playlist State

    private SoundType _currentMusicType = SoundType.None;
    private AudioData.SoundEntry _currentPlaylist;
    private int _currentTrackIndex = 0;
    private Coroutine _playlistCoroutine;
    private Coroutine _fadeCoroutine;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Singleton Setup
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // Đảm bảo GameObject này là Root Object (tách khỏi parent nếu có)
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Khởi tạo AudioSources và load settings từ PlayerPrefs
    /// </summary>
    private void Initialize()
    {
        // Nếu chưa có AudioSource, tự động tạo
        if (_musicSource == null)
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = false; // QUAN TRỌNG: Không loop vì ta tự quản lý playlist
            _musicSource.playOnAwake = false;
        }

        if (_sfxSource == null)
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
        }

        // Load Volume từ PlayerPrefs
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, _defaultMusicVolume);
        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, _defaultSFXVolume);

        SetMusicVolume(musicVolume);
        SetSFXVolume(sfxVolume);

        Debug.Log($"[AudioManager] Initialized - Music: {musicVolume:F2}, SFX: {sfxVolume:F2}");
    }

    #endregion

    #region Public API - Music

    /// <summary>
    /// Phát nhạc nền với Playlist Support
    /// LOGIC THÔNG MINH: Nếu đang phát cùng loại nhạc thì KHÔNG reset
    /// </summary>
    public void PlayMusic(SoundType type, bool forceRestart = false)
    {
        if (_audioData == null)
        {
            Debug.LogError("[AudioManager] AudioData chưa được assign!");
            return;
        }

        // KIỂM TRA TRÙNG NHẠC: Nếu đang phát cùng type và không force restart -> bỏ qua
        if (_currentMusicType == type && !forceRestart)
        {
            Debug.Log($"[AudioManager] Đã phát {type} rồi, bỏ qua để tránh reset nhạc.");
            return;
        }

        // QUAN TRỌNG: Ngắt fade coroutine cũ nếu đang chạy (tránh race condition)
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
            Debug.Log("[AudioManager] Đã hủy FadeOut đang chạy để phát nhạc mới.");
        }

        // Reset volume về mức cài đặt (vì fade out có thể đã giảm volume)
        _musicSource.volume = GetMusicVolume();

        // Lấy playlist
        AudioData.SoundEntry entry = _audioData.GetEntry(type);
        if (entry == null || entry.Clips == null || entry.Clips.Count == 0)
        {
            Debug.LogWarning($"[AudioManager] Không tìm thấy playlist cho {type}");
            return;
        }

        // Stop coroutine cũ nếu có
        if (_playlistCoroutine != null)
        {
            StopCoroutine(_playlistCoroutine);
        }

        // Lưu state
        _currentMusicType = type;
        _currentPlaylist = entry;
        _currentTrackIndex = 0;

        // Bắt đầu phát playlist
        _playlistCoroutine = StartCoroutine(PlaylistCoroutine());

        Debug.Log($"[AudioManager] Bắt đầu playlist: {type} ({entry.Clips.Count} tracks)");
    }

    /// <summary>
    /// Coroutine quản lý Playlist (tự động chuyển bài)
    /// </summary>
    private IEnumerator PlaylistCoroutine()
    {
        while (_currentPlaylist != null && _currentPlaylist.Clips.Count > 0)
        {
            // Chọn track
            AudioClip clip;
            if (_currentPlaylist.Randomize)
            {
                // Random
                _currentTrackIndex = Random.Range(0, _currentPlaylist.Clips.Count);
                clip = _currentPlaylist.Clips[_currentTrackIndex];
            }
            else
            {
                // Tuần tự
                clip = _currentPlaylist.Clips[_currentTrackIndex];
            }

            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] Clip tại index {_currentTrackIndex} là null!");
                _currentTrackIndex = (_currentTrackIndex + 1) % _currentPlaylist.Clips.Count;
                yield return null;
                continue;
            }

            // Phát nhạc
            _musicSource.clip = clip;
            _musicSource.Play();
            Debug.Log($"[AudioManager] Playing track {_currentTrackIndex + 1}/{_currentPlaylist.Clips.Count}: {clip.name}");

            // Chờ cho đến khi bài hát kết thúc
            yield return new WaitWhile(() => _musicSource.isPlaying);

            // Chuyển track tiếp theo (nếu không random)
            if (!_currentPlaylist.Randomize)
            {
                _currentTrackIndex = (_currentTrackIndex + 1) % _currentPlaylist.Clips.Count;
            }

            // Delay nhỏ giữa các bài (tùy chọn)
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// Dừng nhạc nền (KHÔNG fade)
    /// </summary>
    public void StopMusic()
    {
        // Stop playlist coroutine
        if (_playlistCoroutine != null)
        {
            StopCoroutine(_playlistCoroutine);
            _playlistCoroutine = null;
        }

        // Stop fade coroutine (nếu đang chạy)
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        _musicSource.Stop();
        _currentMusicType = SoundType.None;
        _currentPlaylist = null;

        Debug.Log("[AudioManager] Stopped music.");
    }

    /// <summary>
    /// Fade Out và dừng nhạc
    /// </summary>
    public void FadeOutAndStop()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        _fadeCoroutine = StartCoroutine(FadeOutCoroutine());
    }

    private IEnumerator FadeOutCoroutine()
    {
        float startVolume = _musicSource.volume;
        float elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / _fadeDuration);
            yield return null;
        }

        _musicSource.volume = 0f;
        StopMusic();

        // Khôi phục volume
        _musicSource.volume = GetMusicVolume();

        _fadeCoroutine = null;
    }

    /// <summary>
    /// Set âm lượng Music và lưu vào PlayerPrefs
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        _musicSource.volume = volume;
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Lấy Music Volume hiện tại
    /// </summary>
    public float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, _defaultMusicVolume);
    }

    #endregion

    #region Public API - SFX

    /// <summary>
    /// Phát SFX (One Shot) - Random nếu có nhiều clip
    /// </summary>
    public void PlaySFX(SoundType type)
    {
        if (_audioData == null)
        {
            Debug.LogError("[AudioManager] AudioData chưa được assign!");
            return;
        }

        AudioData.SoundEntry entry = _audioData.GetEntry(type);
        if (entry == null || entry.Clips == null || entry.Clips.Count == 0)
        {
            return;
        }

        // Chọn clip (random hoặc đầu tiên)
        AudioClip clip = entry.Randomize
            ? entry.Clips[Random.Range(0, entry.Clips.Count)]
            : entry.Clips[0];

        if (clip == null) return;

        float volume = entry.Volume;
        _sfxSource.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// Set âm lượng SFX và lưu vào PlayerPrefs
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        _sfxSource.volume = volume;
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Lấy SFX Volume hiện tại
    /// </summary>
    public float GetSFXVolume()
    {
        return PlayerPrefs.GetFloat(SFX_VOLUME_KEY, _defaultSFXVolume);
    }

    #endregion

    #region Debug Buttons (Odin Inspector)

#if UNITY_EDITOR
    [Title("Debug Tools", TitleAlignment = TitleAlignments.Centered)]

    [Button(ButtonSizes.Medium)]
    [GUIColor(0.3f, 0.8f, 1f)]
    private void TestPlayMusic()
    {
        PlayMusic(SoundType.MenuMusic);
    }

    [Button(ButtonSizes.Medium)]
    [GUIColor(0.3f, 1f, 0.3f)]
    private void TestPlaySFX()
    {
        PlaySFX(SoundType.ButtonClick);
    }

    [Button(ButtonSizes.Medium)]
    [GUIColor(1f, 0.8f, 0.3f)]
    private void TestFadeOut()
    {
        FadeOutAndStop();
    }

    [Button(ButtonSizes.Medium)]
    [GUIColor(1f, 0.3f, 0.3f)]
    private void ResetVolume()
    {
        SetMusicVolume(_defaultMusicVolume);
        SetSFXVolume(_defaultSFXVolume);
        Debug.Log("[AudioManager] Volume đã được reset về mặc định.");
    }
#endif

    #endregion
}
