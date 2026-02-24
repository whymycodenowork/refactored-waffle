using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The Level class is used to display and handle the logic for the game level.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Level : MonoBehaviour
{
    // TODO: make player render
    public static Tile[,,] tiles =
    {
        { { new(1), new(1), new(), new(1) }, { new(2), new(), new(), new() },  { new(2), new(2), new(), new() } },
        { { new(1), new(1), new(1), new(1) }, { new(),  new(), new(), new() },  { new(),  new(), new(), new() } },
        { { new(1), new(1), new(1), new(1) }, { new(),  new(), new(), new() },  { new(),  new(), new(), new() } },
        { { new(1), new(1), new(1), new(1) }, { new(),  new(), new(), new() },  { new(),  new(), new(), new() } },
        { { new(1), new(1), new(1), new(1) }, { new(),  new(), new(), new() },  { new(),  new(), new(), new() } }
    };

    /// <summary>
    /// the positions of the players' tiles
    /// </summary>
    public static List<Vector3Int> playerTiles = new();

    /// <summary>
    /// the gameobject that is the player
    /// </summary>
    public GameObject playerObject;

    /// <summary>
    /// the player's mesh
    /// </summary>
    public Mesh playerMesh;

    [Header("Movement Arrow Settings")]
    public MovementArrow forwardArrow;  // assign
    public MovementArrow backArrow;     // in
    public MovementArrow rightArrow;    // the
    public MovementArrow leftArrow;     // inspector
    public float arrowHeightOffset = -0.3f; // How high above the ground to place the arrows 

    private static readonly List<Vector3> vertices = new();
    private static readonly List<int> triangles = new();
    private static readonly List<Vector2> uvs = new();
    private static readonly List<Vector3> normals = new();

    private static Mesh mesh;
    private bool rolling = false;
    /// <summary>
    /// how fast the rolling animation is in degrees per second.
    /// </summary>
    public float rollSpeed;

    // debug visualization for failed rolls
    private readonly List<Vector3Int> debugFailedPositions = new();
    private float debugDrawUntil = 0f;

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

        playerObject = new GameObject("Player");
        playerObject.transform.parent = transform;

        MeshFilter mf = playerObject.AddComponent<MeshFilter>();
        MeshRenderer mr = playerObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;

        // make the player red
        mr.material.color = Color.red;

        playerMesh = new Mesh();
        mf.mesh = playerMesh;

        Regenerate();
    }

    /// <summary>
    /// regenerates the meshes
    /// </summary>
    private void Regenerate()
    {
        FindPlayerTiles();
        BuildPlayerMesh(); // run the sync parts first so no race conditions
        PositionPlayer();
        forwardArrow.transform.position = FindPivot(Vector3Int.forward) + Vector3Int.forward + (arrowHeightOffset * Vector3.up);
        backArrow.transform.position = FindPivot(Vector3Int.back) + Vector3Int.back + (arrowHeightOffset * Vector3.up);
        rightArrow.transform.position = FindPivot(Vector3Int.right) + Vector3Int.right + (arrowHeightOffset * Vector3.up);
        leftArrow.transform.position = FindPivot(Vector3Int.left) + Vector3Int.left + (arrowHeightOffset * Vector3.up);
        CreateLevel(tiles, vertices, triangles, uvs, normals, mesh);
    }

    /// <summary>
    /// positions the player 
    /// </summary>
    /// <remarks>
    /// i fixed it (mostly)
    /// </remarks>
    private void PositionPlayer()
    {
        // find the smallest x, y, z of the player tiles
        Vector3Int min = new(int.MaxValue, int.MaxValue, int.MaxValue);

        foreach (Vector3Int pos in playerTiles)
        {
            min = Vector3Int.Min(min, pos);
        }

        playerObject.transform.localPosition = new();
    }

    private void FindPlayerTiles()
    {
        playerTiles.Clear();

        for (int x = 0; x < tiles.GetLength(0); x++)
        {
            for (int y = 0; y < tiles.GetLength(1); y++)
            {
                for (int z = 0; z < tiles.GetLength(2); z++)
                {
                    if (tiles[x, y, z].Type == Tile.TileType.Player)
                    {
                        playerTiles.Add(new Vector3Int(x, y, z));
                    }
                }
            }
        }
    }

    private void BuildPlayerMesh()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        normals.Clear();

        /* 
         * player usually has very few tiles so no need async
         * lets hope that no one makes a level where the player is massive (someone probably will)
         * when push comes to shove i can always make this async and parallel
         */
        foreach (Vector3Int pos in playerTiles)
        {
            foreach (Vector3Int dir in directions)
            {
                if (IsPlayerFaceVisible(pos, dir))
                {
                    AddQuad(pos, dir);
                }
            }
        }

        playerMesh.Clear();
        playerMesh.SetVertices(vertices);
        playerMesh.SetTriangles(triangles, 0);
        playerMesh.SetUVs(0, uvs);
        playerMesh.SetNormals(normals);
        playerObject.transform.rotation = Quaternion.identity; // reset rotation
    }

    private bool IsPlayerFaceVisible(Vector3Int pos, Vector3Int dir)
    {
        Vector3Int n = pos + dir;

        return n.x < 0 || n.x >= tiles.GetLength(0) ||
            n.y < 0 || n.y >= tiles.GetLength(1) ||
            n.z < 0 || n.z >= tiles.GetLength(2) || tiles[n.x, n.y, n.z].Type != Tile.TileType.Player;
    }

    public async void CreateLevel(Tile[,,] tiles, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Vector3> normals, Mesh mesh)
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        normals.Clear();
        // put the presumably slow part in a separate thread
        await Task.Run(() =>
        {
            // loop through all the tiles
            Vector3Int pos = new();
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                for (int y = 0; y < tiles.GetLength(1); y++)
                {
                    for (int z = 0; z < tiles.GetLength(2); z++)
                    {
                        // process each tile
                        pos.Set(x, y, z);
                        Tile tile = tiles[x, y, z];

                        if (tile.Type is Tile.TileType.Empty or Tile.TileType.Player)
                        {
                            continue;
                        }

                        // is foreach slower for arrays? probably negligible
                        foreach (Vector3Int dir in directions) // check all faces of the tile
                        {
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
        mesh.SetNormals(normals); // no need for RecalculateNormals since we have them and this is much faster
        Vector3 vector3 = new(tiles.GetLength(0), tiles.GetLength(1), tiles.GetLength(2));
        mesh.bounds = new Bounds(vector3 / 2, vector3); // simpler bounds calculation
    }

    /// <summary>
    /// all of this is old code and idk how it works but it works so please don't touch it
    /// </summary>
    /// <param name="pos">the position to put the quad</param>
    /// <param name="dir">the direction the quad should face</param>
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
        uvs.Add(new Vector2(0, 1)); // top-left
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
            return tiles[neighborPos.x, neighborPos.y, neighborPos.z].Type is Tile.TileType.Empty or Tile.TileType.Player;
        }

        return true; // if the neighbor is out of bounds then the face is visible
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

    public void OnArrowClicked(Vector3Int direction)
    {
        if (!rolling)
        {
            RollPlayer(direction);
        }
        else
        {
            Debug.Log("dude it's still rolling you don't gotta click so fast");
        }
    }
    private void RollPlayer(Vector3Int dir)
    {
        if (playerTiles.Count == 0)
        {
            return;
        }

        // 1. Find pivot
        Vector3Int pivot = FindPivot(dir);

        // 2. Compute rotated positions
        List<Vector3Int> newPositions = new();
        foreach (Vector3Int tile in playerTiles)
        {
            Vector3Int rel = tile - pivot;
            Vector3Int rotated = rel;

            if (dir == Vector3Int.forward || dir == Vector3Int.back)
            {
                rotated = new Vector3Int(rel.x, -rel.z, rel.y);
                if (dir == Vector3Int.back)
                {
                    rotated = new Vector3Int(rel.x, rel.z, -rel.y);
                }
            }
            else if (dir == Vector3Int.left || dir == Vector3Int.right)
            {
                rotated = new Vector3Int(-rel.y, rel.x, rel.z);
                if (dir == Vector3Int.right)
                {
                    rotated = new Vector3Int(rel.y, -rel.x, rel.z);
                }
            }

            rotated += dir;       // move forward in roll direction
            rotated += pivot;     // translate back to world
            newPositions.Add(rotated);
        }

        // 3. Check collisions
        debugFailedPositions.Clear();
        bool failed = false;

        foreach (Vector3Int pos in newPositions)
        {
            bool invalid =
                pos.x < 0 || pos.x >= tiles.GetLength(0) ||
                pos.y < 0 || pos.y >= tiles.GetLength(1) ||
                pos.z < 0 || pos.z >= tiles.GetLength(2) ||
                (!playerTiles.Contains(pos) && tiles[pos.x, pos.y, pos.z].Type != Tile.TileType.Empty);

            if (invalid)
            {
                failed = true;
                debugFailedPositions.Add(pos);
            }
        }

        if (failed)
        {
            debugDrawUntil = Time.time + 1.5f; // draw for 1.5 seconds
            Debug.Log("invalid roll!");
            return;
        }

        // 4. Move the tiles (preserve data)
        Dictionary<Vector3Int, Tile> tileMap = new(); // old -> new
        for (int i = 0; i < playerTiles.Count; i++)
        {
            tileMap[newPositions[i]] = tiles[playerTiles[i].x, playerTiles[i].y, playerTiles[i].z];
            tiles[playerTiles[i].x, playerTiles[i].y, playerTiles[i].z] = new Tile(Tile.TileType.Empty);
        }

        playerTiles.Clear();
        foreach (KeyValuePair<Vector3Int, Tile> kv in tileMap)
        {
            tiles[kv.Key.x, kv.Key.y, kv.Key.z] = kv.Value;
            playerTiles.Add(kv.Key);
        }



        _ = StartCoroutine(RollAnimation(new Vector3(pivot.x + (dir.x * 0.5f), pivot.y - 0.5f, pivot.z + (dir.z * 0.5f)), dir));
    }

    private IEnumerator RollAnimation(Vector3 pivot, Vector3Int dir)
    {
        rolling = true;

        float degreesLeft = 90f;

        Vector3 axis = Vector3.Cross(Vector3.up, dir);

        while (degreesLeft > 0f)
        {
            // why debug stuff is always either red, green, or magenta?
            Debug.DrawRay(pivot, axis, Color.red, 0.1f); // axis of rotation
            Debug.DrawRay(pivot, dir, Color.green, 0.1f); // direction of roll
            float rotationThisFrame = Mathf.Min(Time.deltaTime * rollSpeed, degreesLeft);
            playerObject.transform.RotateAround(pivot, axis, rotationThisFrame);
            degreesLeft -= rotationThisFrame;
            yield return null;
        }
        rolling = false;
        Regenerate();
    }

    /// <summary>
    /// finds the pivot tile for rolling based on the direction of the roll
    /// </summary>
    /// <param name="dir">the direction of the roll</param>
    /// <returns>the pivot tile</returns>
    private Vector3Int FindPivot(Vector3Int dir)
    {
        Vector3Int best = playerTiles[0];
        bool found = false;

        foreach (Vector3Int tile in playerTiles)
        {
            Vector3Int below = tile + Vector3Int.down;
            Tile tileBelow = (below.x >= 0 && below.x < tiles.GetLength(0) &&
                            below.y >= 0 && below.y < tiles.GetLength(1) &&
                            below.z >= 0 && below.z < tiles.GetLength(2))
                            ? tiles[below.x, below.y, below.z]
                            : new Tile(Tile.TileType.Empty);

            bool supported =
                tileBelow.IsSolid &&
                !tileBelow.IsPlayer;

            if (!supported)
            {
                continue;
            }

            if (!found)
            {
                best = tile;
                found = true;
                continue;
            }

            // choose tile farthest in roll direction
            if (Vector3.Dot(tile, dir) >
                Vector3.Dot(best, dir))
            {
                best = tile;
            }
        }

        // fallback (should rarely trigger)
        if (!found)
        {
            best = playerTiles[0];
        }

        return best;
    }

    private void OnDrawGizmos()
    {
        if (debugFailedPositions == null)
        {
            return;
        }

        if (Time.time > debugDrawUntil)
        {
            return;
        }

        Gizmos.color = Color.magenta;

        foreach (Vector3Int pos in debugFailedPositions)
        {
            // draw cube where the tile would be be
            Gizmos.DrawWireCube(
                pos,
                Vector3.one
            );
        }
    }
}