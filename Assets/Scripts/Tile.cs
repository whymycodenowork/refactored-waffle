/// <summary>
/// The Tile struct represents a single tile in the game level.
/// </summary>
public struct Tile
{
    /// <summary>
    /// The type of the tile used for game logic and rendering.
    /// </summary>
    public TileType Type;

    /// <summary>
    /// info about the tile such as player group id
    /// </summary>
    public byte value;

    public bool conductive;

    

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="type">type</param>
    /// <param name="value">value</param>
    /// <param name="conductive">conductive or not</param>
    public Tile(TileType type, byte value = 0, bool conductive = false)
    {
        Type = type;
        this.value = value;
        this.conductive = conductive;
    }

    /// <summary>
    /// alternate constructor without needing enum
    /// </summary>
    /// <param name="type">type but byte</param>
    /// <param name="value">value</param>
    /// <param name="conductive">conductive or not</param>
    public Tile(byte type, byte value = 0, bool conductive = false)
    {
        Type = (TileType)type;
        this.value = value;
        this.conductive = conductive;
    }

    public enum TileType : byte
    {
        Empty,
        Solid,
        Player,
        Hazard,
        Conductive,
        Goal
    }

    public readonly bool IsSolid => Type != TileType.Empty;

    public readonly bool IsPlayer => Type == TileType.Player;

    // logic is handled elsewhere
}
