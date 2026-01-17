using UnityEngine;

public static class WorldConfig
{
    // Kích thước Chunk (9x9)
    public const int CHUNK_SIZE = 9;
    public const int CHUNK_MIN = -9;
    public const int CHUNK_MAX = 9;

    // Bán kính (4) -> Từ tâm ra cạnh
    public const int HALF_SIZE = 4;

    // Độ dài đường đi
    public const int SEGMENT_LONG = 5;
    public const int SEGMENT_SHORT = 3;
    //Độ dài ban đầu của đường chính
    public const int INITIAL_PATH_LENGTH = 4;

    public const float CHANCE_SPLIT = 0.3f;
    public const float CHANCE_FOREST = 0.2f;
    public const float CHANCE_WATER = 0.05f;

    // Safety loop
    public const int MAX_GENERATION_STEPS = 30;

}
