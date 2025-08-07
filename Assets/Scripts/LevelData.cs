using UnityEngine;

/// <summary>
/// The LevelData struct is used to store the data for a level in the game.
/// </summary>
public struct LevelData
{
    /// <summary>
    /// The tiles in the level.
    /// </summary>
    public Tile[,,] tiles;

    /// <summary>
    /// The player groups in the level.
    /// </summary>
    public PlayerGroup[] playerGroups;
}
