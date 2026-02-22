using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;

/// <summary>
/// CONCRETE CLASS: EnemyBat - Enemy bay với animation system và attack logic.
/// Kế thừa EnemyBase và override các hook methods.
/// ANIMATION: Place (bay), TurnLeft, TurnRight, TakeDame, Attack.
/// BEHAVIOR: Vừa bay vừa "nhìn ngó" (Look Around), tấn công khi gần Home.
/// </summary>
public class EnemyBat : EnemyBase
{
    #region Bat Configuration

    [Title("Bat Settings", TitleAlignment = TitleAlignments.Centered)]

    [BoxGroup("Flight")]
    [Tooltip("Độ cao bay (Y offset khi spawn)")]
    [Range(1f, 5f)]
    [SerializeField] private float flyHeight = 2f;

    [BoxGroup("Combat")]
    [Tooltip("Tầm tấn công (khoảng cách phát hiện Home)")]
    [Range(1f, 10f)]
    [SerializeField] private float attackRange = 3f;

    [BoxGroup("Combat")]
    [Tooltip("Layer của Home (để raycast attack)")]
    [SerializeField] private LayerMask homeLayerMask;

    [BoxGroup("Animation")]
    [Required]
    [Tooltip("Animator component")]
    [SerializeField] private Animator _animator;

    #endregion

    #region Animation System

    // Animation State Hashes (cache để tránh string comparison mỗi frame)
    private static readonly int PlaceHash = Animator.StringToHash("Place");
    private static readonly int TurnLeftHash = Animator.StringToHash("TurnLeft");
    private static readonly int TurnRightHash = Animator.StringToHash("TurnRight");
    private static readonly int TakeDameHash = Animator.StringToHash("TakeDame");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    // Track animation hiện tại để tránh CrossFade trùng lặp
    private int _currentAnimHash = PlaceHash;

    // Look Around Timer
    private float lookAroundTimer = 0f;
    private const float LookAroundIntervalMin = 3f;
    private const float LookAroundIntervalMax = 6f;

    // Flags
    private bool isPlayingLookAround = false;
    private bool isPlayingAttack = false;

    // CancellationToken để hủy UniTask khi return to pool
    private CancellationTokenSource _cancellationTokenSource;

    #endregion

    #region Lifecycle Overrides

    protected override void OnSpawnComplete()
    {
        base.OnSpawnComplete();

        // Khởi tạo CancellationTokenSource mới mỗi khi spawn
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        // Nâng vị trí lên flyHeight
        transform.position += Vector3.up * flyHeight;

        // Reset animation về Place
        PlayAnimation(PlaceHash);

        // Reset Look Around timer
        lookAroundTimer = Random.Range(LookAroundIntervalMin, LookAroundIntervalMax);
        isPlayingLookAround = false;
        isPlayingAttack = false;
    }

    protected override void UpdateMovement()
    {
        // RAYCAST ATTACK: Kiểm tra xem có Home ở gần không
        if (!isPlayingAttack && DetectHomeInRange())
        {
            // Phát hiện Home -> Play Attack animation (KHÔNG dừng di chuyển)
            StartAttackSequence(_cancellationTokenSource.Token).Forget();
        }

        // Di chuyển bình thường (gọi base logic)
        // Vẫn tiếp tục di chuyển ngay cả khi đang attack
        base.UpdateMovement();

        // Look Around Timer: Vừa bay vừa nhìn ngó
        if (!isPlayingLookAround && !isPlayingAttack)
        {
            lookAroundTimer -= Time.deltaTime;

            if (lookAroundTimer <= 0f)
            {
                // Hết timer -> Thực hiện Look Around animation
                PlayLookAroundAnimation(_cancellationTokenSource.Token).Forget();

                // Reset timer
                lookAroundTimer = Random.Range(LookAroundIntervalMin, LookAroundIntervalMax);
            }
        }
    }

    protected override void OnTakeDamage(float amount)
    {
        base.OnTakeDamage(amount);

        // FIX: Tránh lỗi NullReference khi token đã bị clear lúc return to pool
        if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            return;

        // Nếu đang Attack hoặc đã chết thì không play TakeDamage animation
        if (isPlayingAttack || currentState == EnemyState.Dead)
            return;

        // Play animation TakeDame (không block movement)
        PlayTakeDamageAnimation(_cancellationTokenSource.Token).Forget();
    }

    protected override void OnDie()
    {
        base.OnDie();

        // Hủy tất cả UniTask đang chạy
        _cancellationTokenSource?.Cancel();

        // Spawn VFX death tại vị trí enemy (trước khi object bị disabled)
        Vector3 deathPosition = transform.position;
        ObjectPoolManager.Instance.Spawn(PoolType.VFX_Death, deathPosition, Quaternion.identity);
    }

    /// <summary>
    /// Override OnReturnToPool để cleanup CancellationToken.
    /// Gọi khi enemy bị trả về pool (chết hoặc reach base).
    /// </summary>
    public override void OnReturnToPool()
    {
        base.OnReturnToPool();

        // Cleanup CancellationTokenSource
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        // Reset flags
        isPlayingLookAround = false;
        isPlayingAttack = false;
    }

    #endregion

    #region Animation Logic

    /// <summary>
    /// Helper: Play animation nếu khác với animation hiện tại.
    /// Tránh CrossFade spam cùng 1 animation.
    /// SAFETY GUARD: Kiểm tra GameObject active trước khi CrossFade.
    /// </summary>
    private void PlayAnimation(int animHash, float transitionDuration = 0.2f)
    {
        // QUICK FIX: Kiểm tra GameObject có đang active không
        if (!gameObject.activeInHierarchy || _animator == null || !_animator.isActiveAndEnabled)
            return;

        // Chỉ CrossFade nếu khác animation hiện tại
        if (_currentAnimHash != animHash)
        {
            _animator.CrossFade(animHash, transitionDuration);
            _currentAnimHash = animHash;
        }
    }

    /// <summary>
    /// Look Around Animation Sequence (Async UniTask).
    /// TurnLeft -> Place -> TurnRight -> Place.
    /// KHÔNG block movement (chỉ animation).
    /// </summary>
    private async UniTaskVoid PlayLookAroundAnimation(CancellationToken cancellationToken)
    {
        isPlayingLookAround = true;

        try
        {
            // TurnLeft
            PlayAnimation(TurnLeftHash);
            await UniTask.Delay(500, cancellationToken: cancellationToken); // 0.5s

            // Place
            PlayAnimation(PlaceHash);
            await UniTask.Delay(300, cancellationToken: cancellationToken); // 0.3s

            // TurnRight
            PlayAnimation(TurnRightHash);
            await UniTask.Delay(500, cancellationToken: cancellationToken); // 0.5s

            // Place (quay lại bay bình thường)
            PlayAnimation(PlaceHash);
        }
        catch (System.OperationCanceledException)
        {
            // Task bị hủy (enemy chết/return to pool) - không làm gì
        }
        finally
        {
            isPlayingLookAround = false;
        }
    }

    /// <summary>
    /// TakeDamage Animation (Ngắn gọn, không block movement).
    /// TakeDame -> đợi 0.2s -> Place.
    /// </summary>
    private async UniTaskVoid PlayTakeDamageAnimation(CancellationToken cancellationToken)
    {
        try
        {
            PlayAnimation(TakeDameHash, 0.1f); // Transition nhanh
            await UniTask.Delay(200, cancellationToken: cancellationToken); // 0.2s
            PlayAnimation(PlaceHash, 0.1f);
        }
        catch (System.OperationCanceledException)
        {
            // Task bị hủy - không làm gì
        }
    }

    #endregion

    #region Combat Logic

    /// <summary>
    /// Phát hiện Home trong tầm attack bằng SphereCast.
    /// </summary>
    private bool DetectHomeInRange()
    {
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;

        // SphereCast để phát hiện Home
        bool hit = Physics.SphereCast(
            origin,
            0.5f, // Bán kính sphere
            direction,
            out RaycastHit hitInfo,
            attackRange,
            homeLayerMask
        );

        return hit;
    }

    /// <summary>
    /// Attack Sequence (Async UniTask).
    /// Play Attack animation -> Tiếp tục di chuyển tới đích -> Die khi reach base.
    /// </summary>
    private async UniTaskVoid StartAttackSequence(CancellationToken cancellationToken)
    {
        isPlayingAttack = true;

        try
        {
            // Play Attack animation
            PlayAnimation(AttackHash);

            // Đợi animation Attack chạy xong (giả sử 1s)
            await UniTask.Delay(1000, cancellationToken: cancellationToken);

            // Quay lại animation Place để tiếp tục bay
            PlayAnimation(PlaceHash);
        }
        catch (System.OperationCanceledException)
        {
            // Task bị hủy - không làm gì
        }
        finally
        {
            // Cho phép Look Around tiếp tục
            isPlayingAttack = false;

            // Lưu ý: Enemy sẽ tiếp tục di chuyển tới đích,
            // khi reach base thì HandleReachedBase() sẽ tự động gọi và die.
        }
    }

    #endregion

    #region Debug Visualization

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        // Vẽ Attack Range (SphereCast)
        Gizmos.color = Color.red;
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;
        Vector3 endPoint = origin + direction * attackRange;

        Gizmos.DrawLine(origin, endPoint);
        Gizmos.DrawWireSphere(endPoint, 0.5f); // Bán kính SphereCast
    }

    #endregion

    #region Debug Buttons (Odin Inspector)

#if UNITY_EDITOR
    [Title("Debug Animation Tests")]

    [BoxGroup("Debug"), Button("▶ Anim: Place"), GUIColor(0.5f, 1f, 0.5f)]
    private void TestAnimPlace()
    {
        PlayAnimation(PlaceHash);
    }

    [BoxGroup("Debug"), Button("◀ Anim: TurnLeft"), GUIColor(0.5f, 0.5f, 1f)]
    private void TestAnimTurnLeft()
    {
        PlayAnimation(TurnLeftHash);
    }

    [BoxGroup("Debug"), Button("Anim: TurnRight ▶"), GUIColor(0.5f, 0.5f, 1f)]
    private void TestAnimTurnRight()
    {
        PlayAnimation(TurnRightHash);
    }

    [BoxGroup("Debug"), Button("⚠ Anim: TakeDame"), GUIColor(1f, 1f, 0.5f)]
    private void TestAnimTakeDamage()
    {
        if (_cancellationTokenSource != null)
            PlayTakeDamageAnimation(_cancellationTokenSource.Token).Forget();
    }

    [BoxGroup("Debug"), Button("⚔ Anim: Attack"), GUIColor(1f, 0.5f, 0.5f)]
    private void TestAnimAttack()
    {
        PlayAnimation(AttackHash);
    }

    [BoxGroup("Debug"), Button("👀 Test Look Around Sequence"), GUIColor(0.7f, 0.7f, 1f)]
    private void TestLookAround()
    {
        if (_cancellationTokenSource != null)
            PlayLookAroundAnimation(_cancellationTokenSource.Token).Forget();
    }
#endif

    #endregion
}
