using System;
using UnityEngine;
using TMPro;

/// <summary>
/// UI hiển thị thông tin chi tiết của Tháp khi người chơi click vào.
/// ARCHITECTURE:
/// - Component này được gắn vào child của _editCanvas (World Space) trong Tower Prefab.
/// - Nhận dữ liệu qua BindTowerData() từ TowerInteractionManager.
/// - Tự động refresh khi Tower nâng cấp nhờ subscribe vào OnTowerUpgraded event (Observer Pattern).
/// 
/// UI STRUCTURE (2 Panels):
/// - Panel 1 (Info): Tên tháp, Mô tả, Giá bán, Level hiện tại
/// - Panel 2 (Stats): Damage, Range, Fire Rate, Giá nâng cấp
/// 
/// NOTE: 
/// - Buttons (Upgrade/Sell) được gán sự kiện OnClick trực tiếp trong Unity Editor 
///   tới TowerBase.OnUpgradeClicked() và TowerBase.OnSellClicked().
/// - Script này CHỈ hiển thị dữ liệu, KHÔNG xử lý logic button.
/// </summary>
public class TowerEditUI : MonoBehaviour
{
    #region Inspector Fields - Panel 1: Tower Info

    [Header("Panel 1: Tower Info")]
    [Tooltip("Text hiển thị tên tháp")]
    [SerializeField] private TextMeshProUGUI _nameText;

    [Tooltip("Text hiển thị mô tả tháp")]
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [Tooltip("Text hiển thị giá bán (80% TotalSpent)")]
    [SerializeField] private TextMeshProUGUI _sellPriceText;

    [Tooltip("Text hiển thị Level hiện tại (VD: Lv 2 / 3)")]
    [SerializeField] private TextMeshProUGUI _levelText;

    #endregion

    #region Inspector Fields - Panel 2: Stats & Upgrade

    [Header("Panel 2: Stats & Upgrade")]
    [Tooltip("Text hiển thị Damage hiện tại")]
    [SerializeField] private TextMeshProUGUI _damageText;

    [Tooltip("Text hiển thị Attack Range hiện tại")]
    [SerializeField] private TextMeshProUGUI _rangeText;

    [Tooltip("Text hiển thị Fire Rate hiện tại")]
    [SerializeField] private TextMeshProUGUI _fireRateText;

    [Tooltip("Text hiển thị giá nâng cấp")]
    [SerializeField] private TextMeshProUGUI _upgradeCostText;

    #endregion

    #region Private State

    /// <summary>
    /// Reference đến Tower hiện tại đang được hiển thị.
    /// CRITICAL: Cần lưu để unsubscribe event khi Destroy hoặc Rebind.
    /// </summary>
    private TowerBase _currentTower;

    #endregion

    #region Public API

    /// <summary>
    /// Gắn dữ liệu của Tower vào UI này và subscribe event để tự động update.
    /// CALLED BY: TowerInteractionManager khi player click vào tower.
    /// </summary>
    /// <param name="tower">Tower đang được chọn</param>
    public void BindTowerData(TowerBase tower)
    {
        // Unsubscribe khỏi Tower cũ (nếu có)
        UnsubscribeFromTower();

        // Lưu reference Tower mới
        _currentTower = tower;

        if (_currentTower == null)
        {
            Debug.LogWarning("[TowerEditUI] BindTowerData nhận tower NULL! Không thể hiển thị UI.");
            return;
        }

        // Subscribe vào event OnTowerUpgraded để auto-refresh UI khi nâng cấp
        _currentTower.OnTowerUpgraded += RefreshUI;

        // Cập nhật UI lần đầu
        RefreshUI();
    }

    #endregion

    #region Unity Lifecycle

    private void OnDestroy()
    {
        // Unsubscribe để tránh memory leak
        UnsubscribeFromTower();
    }

    #endregion

    #region Private Methods - UI Update

    /// <summary>
    /// Cập nhật toàn bộ UI dựa trên dữ liệu hiện tại của Tower.
    /// CALLED BY: BindTowerData() lần đầu + OnTowerUpgraded event callback.
    /// </summary>
    private void RefreshUI()
    {
        if (_currentTower == null)
        {
            Debug.LogWarning("[TowerEditUI] RefreshUI: _currentTower NULL! Không thể refresh UI.");
            return;
        }

        // === Panel 1: Tower Info ===
        if (_nameText != null)
            _nameText.text = _currentTower.TowerName;

        if (_descriptionText != null)
            _descriptionText.text = _currentTower.Description;

        if (_sellPriceText != null)
        {
            // SỬA ĐỔI: Dùng TotalSpent thay vì BuildCost để tính giá bán
            // (Công bằng với số cấp đã nâng - nếu upgrade nhiều thì bán được giá cao hơn)
            int sellPrice = Mathf.FloorToInt(_currentTower.TotalSpent * 0.8f);
            _sellPriceText.text = $"Sell Price: {sellPrice}";
        }

        if (_levelText != null)
        {
            // Hiển thị: "Lv 2 / 3" hoặc "MAX" nếu đã đạt maxLevel
            if (_currentTower.CurrentLevel >= _currentTower.MaxLevel)
            {
                _levelText.text = $"Lv MAX ({_currentTower.MaxLevel})";
            }
            else
            {
                _levelText.text = $"Lv {_currentTower.CurrentLevel} / {_currentTower.MaxLevel}";
            }
        }

        // === Panel 2: Stats & Upgrade ===
        if (_damageText != null)
            _damageText.text = $"Damage: {_currentTower.Damage:F1}";

        if (_rangeText != null)
            _rangeText.text = $"Attack Range: {_currentTower.AttackRange:F1}";

        if (_fireRateText != null)
            _fireRateText.text = $"Fire Rate: {_currentTower.FireRate:F2}s";

        if (_upgradeCostText != null)
        {
            if (_currentTower.CurrentLevel >= _currentTower.MaxLevel)
            {
                _upgradeCostText.text = "MAX";
            }
            else
            {
                _upgradeCostText.text = $"Upgrade Cost: {_currentTower.UpgradeCost}";
            }
        }

        // NOTE: Không còn check Button.interactable vì Button được gán OnClick trực tiếp trong Editor
        // TowerBase.OnUpgradeClicked() và OnSellClicked() sẽ tự validate logic bên trong
    }

    /// <summary>
    /// Hủy subscribe khỏi Tower hiện tại để tránh memory leak.
    /// </summary>
    private void UnsubscribeFromTower()
    {
        if (_currentTower != null)
        {
            _currentTower.OnTowerUpgraded -= RefreshUI;
            _currentTower = null;
        }
    }

    #endregion
}
