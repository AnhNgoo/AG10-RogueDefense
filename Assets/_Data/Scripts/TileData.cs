using System;
using UnityEngine;

/// <summary>
/// Enum định nghĩa các loại Tile Terrain trong game
/// </summary>
public enum GroundType
{
    Dirt,
    Path
}

public enum SurfaceType
{
    None,
    Grass,
    Water,
    Forest
}

/// <summary>
/// Enum định nghĩa các loại Structure có thể đặt trên Tile
/// </summary>
public enum StructureType
{
    None,
    Home,
    Tower,
    Decoration
}

/// <summary>
/// Lớp lưu trữ dữ liệu của một Tile
/// </summary>
/// <return> Dữ liệu của Tile</return>
public class TileData
{
    //Vị trí lưới của Tile
    public Vector2Int GridPosition { get; private set; }

    //Định nghĩa 3 lớp dữ liệu
    public GroundType GroundType { get; private set; }
    public SurfaceType SurfaceType { get; private set; }
    public StructureType StructureType { get; private set; }

    /// <summary>
    /// Constructor khởi tạo dữ liệu cho Tile
    /// </summary>
    /// <param name="gridPosition"> Vị trí lưới của Tile</param>
    public TileData(Vector2Int gridPosition)
    {
        GridPosition = gridPosition;
        GroundType = GroundType.Dirt;
        SurfaceType = SurfaceType.None;
        StructureType = StructureType.None;
    }

    public void SetPath()
    {
        GroundType = GroundType.Path;
        SurfaceType = SurfaceType.None;
        StructureType = StructureType.None;
    }

    public void SetSurface(SurfaceType surfaceType)
    {
        if (GroundType != GroundType.Path)
            SurfaceType = surfaceType;
    }

    /// <summary>
    /// // Quái chỉ đi được nếu là Path và không có Công trình chặn (trừ Home)
    /// </summary>
    public bool IsWalkable()
    {
        return GroundType == GroundType.Path;
    }

    /// <summary>
    /// Kiểm tra xem có thể đặt Structure lên Tile không
    /// </summary>
    public bool CanPlaceStructure(StructureType structureType)
    {
        if (structureType == StructureType.None)
            return true;
        //Chỉ có thể đặt Structure lên Tile là Path và không có SurfaceType
        if (GroundType != GroundType.Path) return false;
        //Không đặt đè lên công trình khác (đã có rồi thì thôi)
        if (SurfaceType != SurfaceType.None) return false;

        return true;
    }

    /// <summary>
    /// Đặt Structure lên Tile
    /// </summary>
    public bool PlaceStructure(StructureType structureType)
    {
        if (!CanPlaceStructure(structureType))
            return false;

        StructureType = structureType;
        //Khi đặt nhà/tháp, phải dọn sạch cỏ ở dưới để tránh xuyên hình
        if (structureType != StructureType.None)
        {
            StructureType = StructureType.None;
        }

        return true;
    }
}

