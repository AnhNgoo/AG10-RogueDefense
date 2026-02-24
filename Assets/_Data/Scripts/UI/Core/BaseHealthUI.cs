using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Component UI hiển thị máu Nhà Chính bằng các icon (trái tim, khiên, v.v.).
/// SOLID: Single Responsibility - Chỉ quan tâm Hiển thị, không logic tính toán.
/// PATTERN: Observer - Subscribe vào BaseHealthManager.OnBaseHealthChanged để tự động cập nhật.
/// </summary>
public class BaseHealthUI : MonoBehaviour
{
    #region Inspector Fields

    [Header("References")]
    [Tooltip("Danh sách các icon máu (trái tim, khiên). Index 0 = HP đầu tiên, Index 1 = HP thứ 2, v.v.")]
    [SerializeField] private List<GameObject> _healthIcons = new List<GameObject>();

    [Header("Optional Animation")]
    [Tooltip("Bật animation DOTween khi mất máu (nếu có)")]
    [SerializeField] private bool _enableLoseAnimation = true;

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        // Subscribe vào event của BaseHealthManager
        BaseHealthManager.OnBaseHealthChanged += UpdateHealthUI;
    }

    private void OnDisable()
    {
        // Unsubscribe để tránh memory leak
        BaseHealthManager.OnBaseHealthChanged -= UpdateHealthUI;
    }

    private void Start()
    {
        // Validation
        if (_healthIcons == null || _healthIcons.Count == 0)
        {
            Debug.LogError("[BaseHealthUI] Chưa gán _healthIcons! Hãy kéo các icon máu vào Inspector.");
        }

        // Cập nhật UI ban đầu (nếu BaseHealthManager đã khởi tạo)
        if (BaseHealthManager.Instance != null)
        {
            UpdateHealthUI(BaseHealthManager.Instance.CurrentHealth);
        }
    }

    #endregion

    #region Event Handler (Observer Pattern)

    /// <summary>
    /// Callback được gọi khi BaseHealthManager bắn event OnBaseHealthChanged.
    /// Logic: Bật/Tắt icon dựa trên currentHP.
    /// </summary>
    /// <param name="currentHP">Máu hiện tại của Nhà Chính</param>
    private void UpdateHealthUI(int currentHP)
    {
        if (_healthIcons == null || _healthIcons.Count == 0)
        {
            Debug.LogWarning("[BaseHealthUI] Danh sách _healthIcons rỗng! Không thể cập nhật UI.");
            return;
        }

        // Duyệt qua tất cả các icon máu
        for (int i = 0; i < _healthIcons.Count; i++)
        {
            if (_healthIcons[i] == null) continue;

            // LOGIC: Nếu index < currentHP → Bật icon (còn máu)
            //        Nếu index >= currentHP → Tắt icon (đã mất máu)
            bool shouldBeActive = (i < currentHP);

            // Kiểm tra nếu đang MẤT MÁU (icon đang bật → sắp tắt)
            bool isLosingHP = _healthIcons[i].activeSelf && !shouldBeActive;

            if (isLosingHP && _enableLoseAnimation)
            {
                // Play animation mất máu (nếu có DOTween)
                PlayLoseHPAnimation(_healthIcons[i]);
            }

            _healthIcons[i].SetActive(shouldBeActive);
        }

        Debug.Log($"[BaseHealthUI] Cập nhật UI: {currentHP}/{_healthIcons.Count} HP còn lại");
    }

    #endregion

    #region Optional Animation

    /// <summary>
    /// Play animation khi mất máu (DOTween).
    /// Ví dụ: Scale nhỏ dần, hoặc Fade out.
    /// </summary>
    private void PlayLoseHPAnimation(GameObject icon)
    {
        // Cần import DOTween để sử dụng
        // icon.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack);
        // icon.GetComponent<CanvasGroup>()?.DOFade(0f, 0.3f);

        // Hoặc dùng Animation Clip/Animator
        // icon.GetComponent<Animator>()?.SetTrigger("LoseHP");

        Debug.Log($"[BaseHealthUI] Icon {icon.name} đang mất máu (animation nếu có)");
    }

    #endregion

    #region Context Menu (Debug)

#if UNITY_EDITOR
    [ContextMenu("Test Update UI (3 HP)")]
    private void TestUpdateUI3HP()
    {
        UpdateHealthUI(3);
    }

    [ContextMenu("Test Update UI (1 HP)")]
    private void TestUpdateUI1HP()
    {
        UpdateHealthUI(1);
    }

    [ContextMenu("Test Update UI (0 HP)")]
    private void TestUpdateUI0HP()
    {
        UpdateHealthUI(0);
    }
#endif

    #endregion
}
