using UnityEngine;
using Sirenix.OdinInspector;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// Singleton Manager xử lý toàn bộ âm thanh trong game.
/// Tồn tại qua các scene.
/// OPTIMIZED: Sử dụng UniTask thay vì Coroutine để tối ưu performance.
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

    // UniTask CancellationTokens (thay thế Coroutine)
    private CancellationTokenSource _playlistCts;
    private CancellationTokenSource _fadeCts;
    private CancellationTokenSource _demoSfxCts; // Debounce SFX demo khi kéo slider

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

    private void OnDestroy()
    {
        // Cleanup: Cancel và Dispose tất cả CancellationTokenSource
        _playlistCts?.Cancel();
        _playlistCts?.Dispose();
        _fadeCts?.Cancel();
        _fadeCts?.Dispose();
        _demoSfxCts?.Cancel();
        _demoSfxCts?.Dispose();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Khởi tạo AudioSources và tải cài đặt từ PlayerPrefs.
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
    /// Phát nhạc nền có hỗ trợ playlist.
    /// OPTIMIZED: Sử dụng UniTask thay vì Coroutine.
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
            // Loại nhạc này đang phát rồi, bỏ qua.
            return;
        }

        // QUAN TRỌNG: Ngắt fade task cũ nếu đang chạy (tránh race condition)
        _fadeCts?.Cancel();
        _fadeCts?.Dispose();
        _fadeCts = null;

        // Reset volume về mức cài đặt (vì fade out có thể đã giảm volume)
        _musicSource.volume = GetMusicVolume();

        // Lấy playlist
        AudioData.SoundEntry entry = _audioData.GetEntry(type);
        if (entry == null || entry.Clips == null || entry.Clips.Count == 0)
        {
            Debug.LogWarning($"[AudioManager] Không tìm thấy playlist cho {type}");
            return;
        }

        // Stop playlist task cũ nếu có
        _playlistCts?.Cancel();
        _playlistCts?.Dispose();

        // Lưu state
        _currentMusicType = type;
        _currentPlaylist = entry;
        _currentTrackIndex = 0;

        // Bắt đầu phát playlist với UniTask
        _playlistCts = new CancellationTokenSource();
        PlaylistTask(_playlistCts.Token).Forget();
    }

    /// <summary>
    /// UniTask quản lý Playlist (tự động chuyển bài).
    /// OPTIMIZED: Thay thế Coroutine để giảm GC allocation.
    /// </summary>
    private async UniTaskVoid PlaylistTask(CancellationToken token)
    {
        try
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
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
                    continue;
                }

                // Phát nhạc
                _musicSource.clip = clip;
                _musicSource.Play();

                // Chờ cho đến khi bài hát kết thúc (UniTask WaitWhile)
                await UniTask.WaitWhile(() => _musicSource.isPlaying, cancellationToken: token);

                // Chuyển track tiếp theo (nếu không random)
                if (!_currentPlaylist.Randomize)
                {
                    _currentTrackIndex = (_currentTrackIndex + 1) % _currentPlaylist.Clips.Count;
                }

                // Delay nhỏ giữa các bài (tùy chọn)
                await UniTask.Delay(500, cancellationToken: token);
            }
        }
        catch (System.OperationCanceledException)
        {
            // Task bị cancel (bình thường khi đổi nhạc hoặc stop)
        }
    }

    /// <summary>
    /// Dừng nhạc nền ngay lập tức (không fade).
    /// OPTIMIZED: Cancel UniTask thay vì StopCoroutine.
    /// </summary>
    public void StopMusic()
    {
        // Cancel playlist task
        _playlistCts?.Cancel();
        _playlistCts?.Dispose();
        _playlistCts = null;

        // Cancel fade task (nếu đang chạy)
        _fadeCts?.Cancel();
        _fadeCts?.Dispose();
        _fadeCts = null;

        _musicSource.Stop();
        _currentMusicType = SoundType.None;
        _currentPlaylist = null;
    }

    /// <summary>
    /// Fade out và dừng nhạc.
    /// OPTIMIZED: Sử dụng UniTask thay vì Coroutine.
    /// </summary>
    public void FadeOutAndStop()
    {
        // Cancel fade task cũ (nếu đang chạy)
        _fadeCts?.Cancel();
        _fadeCts?.Dispose();

        // Start fade task mới
        _fadeCts = new CancellationTokenSource();
        FadeOutTask(_fadeCts.Token).Forget();
    }

    /// <summary>
    /// UniTask quản lý fade out effect.
    /// </summary>
    private async UniTaskVoid FadeOutTask(CancellationToken token)
    {
        try
        {
            float startVolume = _musicSource.volume;
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / _fadeDuration);

                // Yield frame (tương đương yield return null)
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
            }

            _musicSource.volume = 0f;
            StopMusic();

            // Khôi phục volume
            _musicSource.volume = GetMusicVolume();
        }
        catch (System.OperationCanceledException)
        {
            // Task bị cancel
        }
    }

    /// <summary>
    /// Đặt âm lượng nhạc và lưu vào PlayerPrefs.
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
    /// Phát một SFX (One Shot).
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
    /// Phát SFX với DEBOUNCE (dùng cho Slider preview).
    /// Nếu gọi liên tục, chỉ phát 1 lần sau khi ngừng 200ms.
    /// </summary>
    public void PlaySFXDemo(SoundType type)
    {
        // Cancel task demo SFX cũ (nếu đang chạy)
        _demoSfxCts?.Cancel();
        _demoSfxCts?.Dispose();

        // Tạo token mới và gọi async
        _demoSfxCts = new CancellationTokenSource();
        PlaySFXDemoAsync(type, _demoSfxCts.Token).Forget();
    }

    /// <summary>
    /// UniTask delay 200ms trước khi phát SFX (debounce logic).
    /// </summary>
    private async UniTaskVoid PlaySFXDemoAsync(SoundType type, CancellationToken token)
    {
        try
        {
            // Chờ 200ms (debounce)
            await UniTask.Delay(200, cancellationToken: token);

            // Nếu không bị cancel, phát SFX
            PlaySFX(type);
        }
        catch (System.OperationCanceledException)
        {
            // Task bị cancel (bình thường khi kéo slider liên tục)
        }
    }

    /// <summary>
    /// Đặt âm lượng SFX và lưu vào PlayerPrefs.
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
