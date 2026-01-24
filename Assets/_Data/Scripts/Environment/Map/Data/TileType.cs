/// <summary>
/// Các loại Tile trong map.
/// Ground: Đất trống, Path: Đường đi, Home: Nhà chính, StartPoint: Điểm bắt đầu, EndPoint: Điểm kết thúc.
/// </summary>
public enum TileType
{
    Ground,      // Đất trống
    Path,        // Đường đi chính
    Home,        // Khu vực nhà chính (3x3)
    StartPoint,  // Điểm bắt đầu (entry)
    EndPoint     // Điểm kết thúc (exit)
}
