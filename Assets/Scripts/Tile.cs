/// <summary>
/// The Tile struct represents a single tile in the game level.
/// </summary>
public struct Tile
{
    /// <summary>
    /// The type of the tile used for game logic and rendering.
    /// </summary>
    public TileType Type;

    public byte value;

    public Tile(TileType type, byte value = 0)
    {
        Type = type;
        this.value = value;
    }

    public Tile(byte type, byte value = 0)
    {
        Type = (TileType)type;
        this.value = value;
    }

    public enum TileType : byte
    {
        Empty,
        Solid,
        Hazard,
        Conductive,
        Goal
    }
}
