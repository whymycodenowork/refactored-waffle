using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The Level class is used to display and handle the logic for the game level.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Level : MonoBehaviour
{
    public static Tile[,,] tiles =
    {
        { { new(1), new(1), new(1) }, { new(), new(), new(1) }, { new(), new(), new() } },
        { { new(1), new(1), new(1) }, { new(), new(), new(1) }, { new(), new(), new(1) } },
        { { new(1), new(1), new(1) }, { new(1), new(), new(1) }, { new(), new(), new(1) } }
    };
    public static List<PlayerGroup> playerGroups;

    public static Player selectedPlayer;

    [Header("Movement Arrow Settings")]
    public MovementArrow forwardArrow;  // Assign in editor
    public MovementArrow backArrow;     // Assign in editor
    public MovementArrow rightArrow;    // Assign in editor
    public MovementArrow leftArrow;     // Assign in editor
    public float arrowHeightOffset = -0.45f; // How high above the ground to place arrows

    private static readonly List<Vector3> vertices = new();
    private static readonly List<int> triangles = new();
    private static readonly List<Vector2> uvs = new();
    private static readonly List<Vector3> normals = new();

    private static Mesh mesh;

    public static Level Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
    }
    public void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        CreateLevel(tiles, vertices, triangles, uvs, normals, mesh);

        playerGroups = new()
        {
            new()
        };
        GameObject p = new("PlayerGroup");
        Player plp = p.AddComponent<Player>();
        plp.group = playerGroups[0];
        plp.pos = Vector3Int.up;
        playerGroups[0].players.Add(plp);

        // Position movement arrows after level is generated
        if (playerGroups != null && playerGroups.Count > 0)
        {
            PositionMovementArrows();
        }
    }

    private void Update()
    {
        // Check for player under mouse click
        if (Input.GetMouseButtonDown(0))
        {
            Player player = GetPlayerUnderMouse();
            if (player != null)
            {
                selectedPlayer = player;
                UpdateMovementArrows(); // Update arrows based on selected player
            }
        }
    }

    public async void CreateLevel(Tile[,,] tiles, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Vector3> normals, Mesh mesh)
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        normals.Clear();
        await Task.Run(() =>
        {
            // Loop through all the blocks
            Vector3Int pos = new();
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                for (int y = 0; y < tiles.GetLength(1); y++)
                {
                    for (int z = 0; z < tiles.GetLength(2); z++)
                    {
                        // Process each tile in the chunk
                        pos.Set(x, y, z);
                        Tile tile = tiles[x, y, z];

                        if (tile.Type == 0)
                        {
                            continue; // Skip empty blocks
                        }

                        for (int i = 0; i < 6; i++) // Check all 6 faces of the tile
                        {
                            Vector3Int dir = directions[i];
                            if (IsFaceVisible(pos, dir))
                            {
                                AddQuad(pos, dir);
                            }
                        }
                    }
                }
            }
        });
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.SetNormals(normals);
        Vector3 vector3 = new(tiles.GetLength(0), tiles.GetLength(1), tiles.GetLength(2));
        mesh.bounds = new Bounds(vector3 / 2, vector3);
    }

    /// <summary>
    /// Positions the 4 movement arrows based on player group positions
    /// </summary>
    public void PositionMovementArrows()
    {
        if (playerGroups == null || playerGroups.Count == 0)
        {
            return;
        }

        // Get the center position of the first player group (assuming single group for now)
        Vector3Int groupCenter = GetPlayerGroupCenter(playerGroups[0]);

        // Position each arrow in its respective direction
        PositionArrow(forwardArrow, groupCenter, Vector3Int.forward);
        PositionArrow(backArrow, groupCenter, Vector3Int.back);
        PositionArrow(rightArrow, groupCenter, Vector3Int.right);
        PositionArrow(leftArrow, groupCenter, Vector3Int.left);
    }

    /// <summary>
    /// Positions a single arrow in the specified direction from the player group
    /// </summary>
    private void PositionArrow(MovementArrow arrow, Vector3Int groupCenter, Vector3Int direction)
    {
        if (arrow == null)
        {
            return;
        }

        Vector3Int arrowPosition = FindArrowPosition(groupCenter, direction);

        if (arrowPosition != Vector3Int.one * -1) // Valid position found
        {
            Vector3 worldPos = new(arrowPosition.x, arrowPosition.y + arrowHeightOffset, arrowPosition.z);
            arrow.transform.position = worldPos;
            arrow.gameObject.SetActive(true);
        }
        else
        {
            // Hide arrow if no valid position
            arrow.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Finds the position where an arrow should be placed (first empty tile from bottom)
    /// </summary>
    private Vector3Int FindArrowPosition(Vector3Int startPos, Vector3Int direction)
    {
        Vector3Int searchPos = startPos + direction;

        // Make sure we're within level bounds
        if (searchPos.x < 0 || searchPos.x >= tiles.GetLength(0) ||
            searchPos.z < 0 || searchPos.z >= tiles.GetLength(2))
        {
            return Vector3Int.one * -1; // Invalid position
        }

        // Find the first empty tile from the bottom
        for (int y = 0; y < tiles.GetLength(1); y++)
        {
            searchPos.y = y;

            if (tiles[searchPos.x, searchPos.y, searchPos.z].Type == 0) // Empty tile
            {
                // Check if there's a solid tile below (or we're at the bottom)
                if (y == 0 || tiles[searchPos.x, y - 1, searchPos.z].Type != 0)
                {
                    return searchPos;
                }
            }
        }

        return Vector3Int.one * -1; // No valid position found
    }

    /// <summary>
    /// Call this when a player moves to update arrow positions
    /// </summary>
    public void UpdateMovementArrows()
    {
        PositionMovementArrows();
    }

    /// <summary>
    /// Handles when an arrow is clicked - moves the associated player group
    /// </summary>
    public void OnArrowClicked(Vector3Int direction)
    {
        if (playerGroups != null && playerGroups.Count > 0)
        {
            selectedPlayer.group.MoveAllPlayers(direction); // Move first player group

            Invoke(nameof(UpdateMovementArrows), 0.5f); // Adjust delay as needed
        }
    }

    private void AddQuad(Vector3Int pos, Vector3Int dir)
    {
        if (dir == Vector3Int.up)
        {
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, 0.5f));
            vertices.Add(pos + new Vector3(0.5f, 0.5f, 0.5f));
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, -0.5f));
            vertices.Add(pos + new Vector3(0.5f, 0.5f, -0.5f));
        }
        else if (dir == Vector3Int.down)
        {
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, -0.5f));
            vertices.Add(pos + new Vector3(0.5f, -0.5f, -0.5f));
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, 0.5f));
            vertices.Add(pos + new Vector3(0.5f, -0.5f, 0.5f));
        }
        else if (dir == Vector3Int.forward)
        {
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, 0.5f));
            vertices.Add(pos + new Vector3(0.5f, -0.5f, 0.5f));
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, 0.5f));
            vertices.Add(pos + new Vector3(0.5f, 0.5f, 0.5f));
        }
        else if (dir == Vector3Int.back)
        {
            vertices.Add(pos + new Vector3(0.5f, -0.5f, -0.5f));
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, -0.5f));
            vertices.Add(pos + new Vector3(0.5f, 0.5f, -0.5f));
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, -0.5f));
        }
        else if (dir == Vector3Int.left)
        {
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, -0.5f));
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, 0.5f));
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, -0.5f));
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, 0.5f));
        }
        else if (dir == Vector3Int.right)
        {
            vertices.Add(pos + new Vector3(0.5f, -0.5f, 0.5f));
            vertices.Add(pos + new Vector3(0.5f, -0.5f, -0.5f));
            vertices.Add(pos + new Vector3(0.5f, 0.5f, 0.5f));
            vertices.Add(pos + new Vector3(0.5f, 0.5f, -0.5f));
        }

        int startIndex = vertices.Count - 4;
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 3);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 0);

        uvs.Add(new Vector2(0, 0)); // bottom-left
        uvs.Add(new Vector2(1, 0)); // bottom-right
        uvs.Add(new Vector2(0, 1));  // top-left
        uvs.Add(new Vector2(1, 1)); // top-right

        normals.Add(dir);
        normals.Add(dir);
        normals.Add(dir);
        normals.Add(dir);
    }

    private bool IsFaceVisible(Vector3Int pos, Vector3Int dir)
    {
        Vector3Int neighborPos = pos + dir;

        if (neighborPos.x >= 0 && neighborPos.x < tiles.GetLength(0) &&
            neighborPos.y >= 0 && neighborPos.y < tiles.GetLength(1) &&
            neighborPos.z >= 0 && neighborPos.z < tiles.GetLength(2))
        {
            return tiles[neighborPos.x, neighborPos.y, neighborPos.z].Type == 0;
        }

        return true; // If the neighbor is out of bounds, the face is visible
    }

    private static readonly Vector3Int[] directions = new Vector3Int[]
    {
        Vector3Int.forward,
        Vector3Int.back,
        Vector3Int.left,
        Vector3Int.right,
        Vector3Int.up,
        Vector3Int.down
    };

    /// <summary>
    /// Gets the center position of a player group
    /// </summary>
    private Vector3Int GetPlayerGroupCenter(PlayerGroup playerGroup)
    {
        // This assumes PlayerGroup has a way to get its position
        // You might need to adjust this based on your PlayerGroup implementation
        if (playerGroup.players != null && playerGroup.players.Count > 0)
        {
            return playerGroup.players[0].pos; // Use first player's position as reference
        }

        return Vector3Int.zero;
    }

    /// <summary>
    /// Casts a ray from the mouse position to find players
    /// </summary>
    /// <returns>The Player component if found, null otherwise</returns>
    public Player GetPlayerUnderMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.TryGetComponent<Player>(out Player player))
            {
                return player;
            }
        }

        return null;
    }
}