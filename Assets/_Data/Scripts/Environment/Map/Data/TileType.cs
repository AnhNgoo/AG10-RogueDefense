/// <summary>
/// Enum loại ô đất (Hiện tại chỉ có Ground và Path).
/// </summary>
public enum TileType
{
    Ground,      // Đất trống
    Path,        // Đường đi chính
    Home,        // Khu vực nhà chính (3x3)
    StartPoint,  // Điểm bắt đầu (entry)
    EndPoint     // Điểm kết thúc (exit)
}
