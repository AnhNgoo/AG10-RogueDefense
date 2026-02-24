using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Component UI hiển thị số vàng (Gold) và animation khi thay đổi.
/// SOLID: Single Responsibility - Chỉ quan tâm Hiển thị và Animation.
/// PATTERN: Observer - Subscribe vào CurrencyManager.OnGoldChanged để tự động cập nhật.
/// OPTIMIZATION: Dùng DOTweenAnimation component (Inspector) thay vì code animation.
/// </summary>
[RequireComponent(typeof(DOTweenAnimation))]
public class CurrencyUI : MonoBehaviour
{
    #region Inspector Fields

    [Header("References")]
    [Tooltip("Text hiển thị số vàng (TextMeshProUGUI)")]
    [SerializeField] private TextMeshProUGUI _goldText;

    [Header("Animation")]
    [Tooltip("Component DOTweenAnimation để làm hiệu ứng Punch Scale")]
    [SerializeField] private DOTweenAnimation _goldPunchAnim;

    [Header("Formatting")]
    [Tooltip("Màu text khi đủ tiền")]
    [SerializeField] private Color _normalColor = Color.white;

    [Tooltip("Màu text khi không đủ tiền (optional feature)")]
    [SerializeField] private Color _insufficientColor = new Color(1f, 0.3f, 0.3f, 1f);

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        // Subscribe vào event của CurrencyManager
        CurrencyManager.OnGoldChanged += UpdateGoldUI;
    }

    private void OnDisable()
    {
        // Unsubscribe để tránh memory leak
        CurrencyManager.OnGoldChanged -= UpdateGoldUI;
    }

    private void Start()
    {
        // Auto-find DOTweenAnimation nếu chưa assign
        if (_goldPunchAnim == null)
        {
            _goldPunchAnim = GetComponent<DOTweenAnimation>();

            if (_goldPunchAnim == null)
            {
                Debug.LogError("[CurrencyUI] Không tìm thấy DOTweenAnimation component! Hãy thêm vào GameObject này.");
            }
        }

        // Validation
        if (_goldText == null)
        {
            Debug.LogError("[CurrencyUI] Chưa assign _goldText! Hãy kéo TextMeshProUGUI vào Inspector.");
        }
    }

    #endregion

    #region Event Handler (Observer Pattern)

    /// <summary>
    /// Callback được gọi khi CurrencyManager bắn event OnGoldChanged.
    /// </summary>
    /// <param name="newAmount">Số vàng MỚI sau khi thay đổi</param>
    private void UpdateGoldUI(int newAmount)
    {
        if (_goldText == null) return;

        // Cập nhật text hiển thị
        _goldText.text = newAmount.ToString();

        // Reset màu về normal (tùy chọn: có thể check affordability sau)
        _goldText.color = _normalColor;

        // Kích hoạt animation Punch Scale bằng DOTweenAnimation component
        PlayPunchAnimation();
    }

    #endregion

    #region Animation Control

    /// <summary>
    /// Kích hoạt animation Punch Scale trên DOTweenAnimation component.
    /// QUAN TRỌNG: Animation được cấu hình sẵn trong Inspector, không code.
    /// </summary>
    private void PlayPunchAnimation()
    {
        if (_goldPunchAnim == null) return;

        // DOTweenAnimation có 2 cách play:
        // - DORestart(): Reset về trạng thái ban đầu rồi play (an toàn hơn)
        // - DOPlay(): Play từ trạng thái hiện tại
        // → Dùng DORestart() để animation luôn hiển thị đầy đủ
        _goldPunchAnim.DORestart();
    }

    #endregion

    #region Public API (Optional - For Manual Control)

    /// <summary>
    /// Hiển thị trạng thái "không đủ tiền" (đổi màu text).
    /// Gọi từ BuildManager khi player cố mua tháp mà không đủ tiền.
    /// </summary>
    public void ShowInsufficientFeedback()
    {
        if (_goldText == null) return;

        // Đổi màu text thành đỏ
        _goldText.color = _insufficientColor;

        // Play shake animation (nếu có thêm DOTweenAnimation khác)
        // Hoặc có thể dùng DOTween code: _goldText.transform.DOShakePosition(...)

        // Reset về màu normal sau 0.5 giây
        DOVirtual.DelayedCall(0.5f, () =>
        {
            if (_goldText != null)
            {
                _goldText.color = _normalColor;
            }
        });
    }

    #endregion

    #region Context Menu (Debug Tools)

#if UNITY_EDITOR
    [ContextMenu("Test Punch Animation")]
    private void TestPunchAnimation()
    {
        PlayPunchAnimation();
    }

    [ContextMenu("Test Insufficient Feedback")]
    private void TestInsufficientFeedback()
    {
        ShowInsufficientFeedback();
    }
#endif

    #endregion
}
