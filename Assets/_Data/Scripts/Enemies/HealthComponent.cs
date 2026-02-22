using System;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

/// <summary>
/// Component quản lý HP cho Enemy.
/// SINGLE RESPONSIBILITY: Chỉ lo về máu và UI health bar.
/// Reusable cho nhiều loại Enemy khác nhau.
/// </summary>
public class HealthComponent : MonoBehaviour
{
    #region Inspector Configuration

    [Title("Health Settings")]
    [BoxGroup("Config")]
    [MinValue(1)]
    [Tooltip("Máu tối đa")]
    [SerializeField] private float _maxHealth = 100f;

    [BoxGroup("UI References")]
    [Required]
    [Tooltip("Canvas World Space chứa health bar")]
    [SerializeField] private Canvas _healthCanvas;

    [BoxGroup("UI References")]
    [Required]
    [Tooltip("Image Fill để hiển thị % máu còn lại")]
    [SerializeField] private Image _healthFillImage;

    #endregion

    #region Runtime Data

    [Title("Runtime Info")]
    [ShowInInspector, ReadOnly, ProgressBar(0, "_maxHealth")]
    [LabelText("Current HP")]
    private float _currentHealth;

    /// <summary>
    /// Event phát khi đối tượng chết (HP <= 0).
    /// Lắng nghe bởi: EnemyBase để chuyển state Dead.
    /// </summary>
    public event Action OnDeath;

    #endregion

    #region Public Properties

    /// <summary>
    /// Máu hiện tại (read-only).
    /// </summary>
    public float CurrentHealth => _currentHealth;

    /// <summary>
    /// Máu tối đa (read-only).
    /// </summary>
    public float MaxHealth => _maxHealth;

    /// <summary>
    /// Kiểm tra đã chết chưa.
    /// </summary>
    public bool IsDead => _currentHealth <= 0f;

    #endregion

    #region Initialization

    /// <summary>
    /// Khởi tạo hoặc reset health về full.
    /// Gọi khi enemy spawn từ pool.
    /// </summary>
    public void Initialize()
    {
        _currentHealth = _maxHealth;
        UpdateHealthUI();
    }

    /// <summary>
    /// Khởi tạo với custom max health.
    /// Dùng cho Enemy có nhiều biến thể khác nhau (Boss, Elite, etc.).
    /// </summary>
    public void Initialize(float customMaxHealth)
    {
        _maxHealth = customMaxHealth;
        _currentHealth = _maxHealth;
        UpdateHealthUI();
    }

    #endregion

    #region Health Management

    /// <summary>
    /// Nhận sát thương.
    /// </summary>
    /// <param name="amount">Lượng sát thương</param>
    public void TakeDamage(float amount)
    {
        if (IsDead) return; // Đã chết rồi, bỏ qua damage

        _currentHealth -= amount;
        _currentHealth = Mathf.Max(_currentHealth, 0f); // Clamp về 0

        UpdateHealthUI();

        // Kiểm tra chết
        if (_currentHealth <= 0f)
        {
            HandleDeath();
        }
    }

    /// <summary>
    /// Hồi máu.
    /// </summary>
    /// <param name="amount">Lượng máu hồi</param>
    public void Heal(float amount)
    {
        if (IsDead) return; // Đã chết thì không hồi được

        _currentHealth += amount;
        _currentHealth = Mathf.Min(_currentHealth, _maxHealth); // Clamp về max

        UpdateHealthUI();
    }

    /// <summary>
    /// Xử lý khi đối tượng chết.
    /// </summary>
    private void HandleDeath()
    {
        // Phát event OnDeath
        // Lưu ý: Không cần disable Canvas vì GameObject sẽ bị disable/return to pool,
        // Canvas là child nên sẽ tự động ẩn theo.
        OnDeath?.Invoke();
    }

    #endregion

    #region UI Updates

    /// <summary>
    /// Cập nhật UI health bar dựa trên % máu còn lại.
    /// </summary>
    private void UpdateHealthUI()
    {
        if (_healthFillImage != null)
        {
            float fillAmount = _currentHealth / _maxHealth;
            _healthFillImage.fillAmount = fillAmount;
        }
    }

    /// <summary>
    /// Billboard Effect: Canvas luôn quay về phía Camera.
    /// Gọi trong LateUpdate để chạy sau tất cả Update/FixedUpdate.
    /// </summary>
    private void LateUpdate()
    {
        if (_healthCanvas != null && _healthCanvas.enabled && Camera.main != null)
        {
            // Xoay Canvas để luôn nhìn về Camera
            _healthCanvas.transform.rotation = Camera.main.transform.rotation;
        }
    }

    #endregion

    #region Debug Helpers

#if UNITY_EDITOR
    [BoxGroup("Debug"), Button("Test TakeDamage(20)"), GUIColor(1f, 0.5f, 0.5f)]
    private void DebugTakeDamage()
    {
        TakeDamage(20f);
    }

    [BoxGroup("Debug"), Button("Test Heal(30)"), GUIColor(0.5f, 1f, 0.5f)]
    private void DebugHeal()
    {
        Heal(30f);
    }

    [BoxGroup("Debug"), Button("Test Death (9999 damage)"), GUIColor(1f, 0f, 0f)]
    private void DebugDeath()
    {
        TakeDamage(9999f);
    }
#endif

    #endregion
}
