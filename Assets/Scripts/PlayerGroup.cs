using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class represents a group of players that move together with the same input.
/// </summary>
public class PlayerGroup
{
    public List<Player> players = new();

    public void MoveAllPlayers(Vector3Int dir)
    {
        foreach (Player player in players)
        {
            player.Roll(dir);
        }
    }
}
