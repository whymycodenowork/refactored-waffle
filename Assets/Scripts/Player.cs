using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents the controllable player shape that rolls on a voxel grid (like Bloxorz).
/// Handles arbitrary (non-cubic) shapes by deriving rotated variants and updating mesh/collider.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Player : MonoBehaviour
{
    public PlayerGroup group; // Reference to the player group this player belongs to
    public byte[,,] shape =
    {
        { { 1 } }
    };                      // Original static shape: dimsX x dimsY x dimsZ
    private byte[,,] rotatedShape;              // Derived rotated shape for logic/rendering

    public Vector3Int pos;                      // Grid position (anchor)

    private bool rolling = false;
    private const float ROLL_SPEED = 180f;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        // Initialize rotatedShape to a copy of shape
        rotatedShape = (byte[,,])shape.Clone();
        UpdateVisualMesh();
    }

    /// <summary>
    /// Rolls the player one tile in the given direction (Grid step).
    /// </summary>
    public void Roll(Vector3Int direction)
    {
        if (rolling)
        {
            return;
        }

        _ = StartCoroutine(RollCoroutine(direction));
    }

    private IEnumerator RollCoroutine(Vector3Int direction)
    {
        rolling = true;

        // Backup old state
        byte[,,] oldShape = (byte[,,])rotatedShape.Clone();
        transform.GetPositionAndRotation(out Vector3 oldPosition, out Quaternion oldRotation);

        // Derive new rotated shape (for collision logic only)
        rotatedShape = RotateShape(oldShape, direction);

        // Compute pivot in world space based on floor-contact
        Vector3 pivot = FindPivot(direction);
        Vector3 axis = Vector3.Cross(Vector3.up, direction);

        // Animate 90° roll
        float remaining = 90f;
        while (remaining > 0f)
        {
            float step = ROLL_SPEED * Time.deltaTime;
            if (step > remaining)
            {
                step = remaining;
            }

            transform.RotateAround(pivot, axis, step);
            remaining -= step;
            yield return null;
        }

        // Snap to grid: update logical position and world transform
        pos += direction;
        transform.SetPositionAndRotation(new Vector3(pos.x, pos.y, pos.z), oldRotation * Quaternion.AngleAxis(90f, axis));

        // Collision check using Physics.CheckBox
        Bounds bounds = meshCollider.bounds;
        if (Physics.CheckBox(bounds.center, bounds.extents, transform.rotation, ~0))
        {
            // Check if we're colliding with something other than ourselves
            Collider[] overlapping = Physics.OverlapBox(bounds.center, bounds.extents, transform.rotation, ~0);
            bool hasCollision = false;
            foreach (Collider col in overlapping)
            {
                if (col != meshCollider)
                {
                    hasCollision = true;
                    break;
                }
            }

            if (hasCollision)
            {
                // Collision: revert
                rotatedShape = oldShape;
                pos -= direction;
                transform.SetPositionAndRotation(oldPosition, oldRotation);
            }
        }

        rolling = false;
    }

    /// <summary>
    /// Rotates the input shape 90° around the appropriate axis for the given direction.
    /// Supports non-cubic shapes by using actual dimensions.
    /// </summary>
    private byte[,,] RotateShape(byte[,,] input, Vector3Int direction)
    {
        int sizeX = input.GetLength(0), sizeY = input.GetLength(1), sizeZ = input.GetLength(2);

        // For rolling, we need to determine new dimensions after rotation
        byte[,,] output;

        if (direction == Vector3Int.forward || direction == Vector3Int.back)
        {
            // Rotating around X-axis: Y and Z dimensions swap
            output = new byte[sizeX, sizeZ, sizeY];
        }
        else if (direction == Vector3Int.right || direction == Vector3Int.left)
        {
            // Rotating around Z-axis: X and Y dimensions swap  
            output = new byte[sizeY, sizeX, sizeZ];
        }
        else
        {
            // No rotation or unsupported direction
            return (byte[,,])input.Clone();
        }

        int outSizeX = output.GetLength(0);
        int outSizeY = output.GetLength(1);
        int outSizeZ = output.GetLength(2);

        // Rotate each filled cell
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    if (input[x, y, z] == 0)
                    {
                        continue;
                    }

                    int newX, newY, newZ;

                    if (direction == Vector3Int.forward)
                    {
                        // 90° around X-axis (forward): (x,y,z) -> (x,z,sizeY-1-y)
                        newX = x;
                        newY = z;
                        newZ = sizeY - 1 - y;
                    }
                    else if (direction == Vector3Int.back)
                    {
                        // -90° around X-axis (back): (x,y,z) -> (x,sizeZ-1-z,y)
                        newX = x;
                        newY = sizeZ - 1 - z;
                        newZ = y;
                    }
                    else if (direction == Vector3Int.right)
                    {
                        // -90° around Z-axis (right): (x,y,z) -> (y,sizeX-1-x,z)
                        newX = y;
                        newY = sizeX - 1 - x;
                        newZ = z;
                    }
                    else if (direction == Vector3Int.left)
                    {
                        // 90° around Z-axis (left): (x,y,z) -> (sizeY-1-y,x,z)
                        newX = sizeY - 1 - y;
                        newY = x;
                        newZ = z;
                    }
                    else
                    {
                        continue;
                    }

                    if (newX >= 0 && newX < outSizeX && newY >= 0 && newY < outSizeY && newZ >= 0 && newZ < outSizeZ)
                    {
                        output[newX, newY, newZ] = input[x, y, z];
                    }
                }
            }
        }

        return output;
    }

    /// <summary>
    /// Finds the pivot point around which the shape should roll, based on floor-contact cells.
    /// </summary>
    private Vector3 FindPivot(Vector3Int direction)
    {
        int sizeX = rotatedShape.GetLength(0), sizeZ = rotatedShape.GetLength(2);
        Vector3 bestOffset = Vector3.zero;
        float bestDot = float.NegativeInfinity;
        Vector3 centerOffset = new((sizeX - 1) / 2f, 0, (sizeZ - 1) / 2f);

        // Check only bottom layer (y=0) for floor contact
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                if (rotatedShape[x, 0, z] == 0)
                {
                    continue;
                }

                Vector3 local = new Vector3(x, 0, z) - centerOffset;
                float dot = Vector3.Dot(local, direction);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestOffset = local;
                }
            }
        }

        // World-space pivot: anchor + bestOffset + half-step in direction - half tile down
        return transform.position + bestOffset + ((Vector3)direction * 0.5f) - (Vector3.up * 0.5f);
    }

    /// <summary>
    /// Rebuilds the mesh from the original shape (only called once at start).
    /// Only generates visible faces (no internal faces between adjacent voxels).
    /// </summary>
    private void UpdateVisualMesh()
    {
        List<Vector3> vertices = new();
        List<int> triangles = new();

        int sizeX = shape.GetLength(0);
        int sizeY = shape.GetLength(1);
        int sizeZ = shape.GetLength(2);

        Vector3 centerOffset = new((sizeX - 1) / 2f, (sizeY - 1) / 2f, (sizeZ - 1) / 2f);

        // Generate faces only for visible sides of each voxel
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    if (shape[x, y, z] == 0)
                    {
                        continue;
                    }

                    Vector3 cubePos = new Vector3(x, y, z) - centerOffset;

                    // Check each face and only add if it's exposed (adjacent voxel is empty or out of bounds)
                    // Front face (+Z)
                    if (z == sizeZ - 1 || shape[x, y, z + 1] == 0)
                    {
                        AddQuad(vertices, triangles, cubePos, Vector3.forward);
                    }

                    // Back face (-Z)
                    if (z == 0 || shape[x, y, z - 1] == 0)
                    {
                        AddQuad(vertices, triangles, cubePos, Vector3.back);
                    }

                    // Right face (+X)
                    if (x == sizeX - 1 || shape[x + 1, y, z] == 0)
                    {
                        AddQuad(vertices, triangles, cubePos, Vector3.right);
                    }

                    // Left face (-X)
                    if (x == 0 || shape[x - 1, y, z] == 0)
                    {
                        AddQuad(vertices, triangles, cubePos, Vector3.left);
                    }

                    // Top face (+Y)
                    if (y == sizeY - 1 || shape[x, y + 1, z] == 0)
                    {
                        AddQuad(vertices, triangles, cubePos, Vector3.up);
                    }

                    // Bottom face (-Y)
                    if (y == 0 || shape[x, y - 1, z] == 0)
                    {
                        AddQuad(vertices, triangles, cubePos, Vector3.down);
                    }
                }
            }
        }

        Mesh mesh = new()
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray()
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    private void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 center, Vector3 normal)
    {
        int startIndex = vertices.Count;
        float size = 0.5f;

        Vector3[] quadVertices = new Vector3[4];

        if (normal == Vector3.forward) // +Z face
        {
            quadVertices[0] = center + new Vector3(-size, -size, size);
            quadVertices[1] = center + new Vector3(size, -size, size);
            quadVertices[2] = center + new Vector3(size, size, size);
            quadVertices[3] = center + new Vector3(-size, size, size);
        }
        else if (normal == Vector3.back) // -Z face
        {
            quadVertices[0] = center + new Vector3(size, -size, -size);
            quadVertices[1] = center + new Vector3(-size, -size, -size);
            quadVertices[2] = center + new Vector3(-size, size, -size);
            quadVertices[3] = center + new Vector3(size, size, -size);
        }
        else if (normal == Vector3.right) // +X face
        {
            quadVertices[0] = center + new Vector3(size, -size, size);
            quadVertices[1] = center + new Vector3(size, -size, -size);
            quadVertices[2] = center + new Vector3(size, size, -size);
            quadVertices[3] = center + new Vector3(size, size, size);
        }
        else if (normal == Vector3.left) // -X face
        {
            quadVertices[0] = center + new Vector3(-size, -size, -size);
            quadVertices[1] = center + new Vector3(-size, -size, size);
            quadVertices[2] = center + new Vector3(-size, size, size);
            quadVertices[3] = center + new Vector3(-size, size, -size);
        }
        else if (normal == Vector3.up) // +Y face
        {
            quadVertices[0] = center + new Vector3(-size, size, -size);
            quadVertices[1] = center + new Vector3(size, size, -size);
            quadVertices[2] = center + new Vector3(size, size, size);
            quadVertices[3] = center + new Vector3(-size, size, size);
        }
        else if (normal == Vector3.down) // -Y face
        {
            quadVertices[0] = center + new Vector3(-size, -size, size);
            quadVertices[1] = center + new Vector3(size, -size, size);
            quadVertices[2] = center + new Vector3(size, -size, -size);
            quadVertices[3] = center + new Vector3(-size, -size, -size);
        }

        vertices.AddRange(quadVertices);

        // Two triangles per quad (counter-clockwise winding)
        triangles.AddRange(new int[]
        {
            startIndex + 0, startIndex + 1, startIndex + 2,
            startIndex + 0, startIndex + 2, startIndex + 3
        });
    }

    private Vector3 GetShapeExtents()
    {
        return new Vector3(rotatedShape.GetLength(0) / 2f,
                           rotatedShape.GetLength(1) / 2f,
                           rotatedShape.GetLength(2) / 2f);
    }
}