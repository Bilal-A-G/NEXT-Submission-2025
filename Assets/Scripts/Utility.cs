using System;
using UnityEngine;

public enum Direction : int
{
    Up,
    Down,
    Left,
    Right,
    Forwards,
    Backwards,
    Length
}

[System.Serializable]
public struct Bounds
{
    [Tooltip("Where the center of the grid is")]
    public Vector3 position;
    
    [Tooltip("How far does the grid extend in the X axis from the center in cells")]
    [Range(0, 100)] public int xExtends;
    [Tooltip("How far does the grid extend in the Y axis from the ground, going up")]
    [Range(0, 100)] public int yExtends;
    [Tooltip("How far does the grid extend in the Z axis from the center in cells")]
    [Range(0, 100)] public int zExtends;
}

public class Utility : MonoBehaviour
{
    public delegate void OnLoop(Vector3Int arrayIndex, Vector3 cellPosition, Vector3 samplePosition);

    //Simple utility so I don't have to type this out a million times
    public static void LoopOverAllCells(OnLoop onLoop, Bounds bounds, float cellScale, float sampleOffset = 1)
    {
        for (int i = -bounds.xExtends; i < bounds.xExtends + 1; i++)
        {
            for (int j = 0; j < bounds.yExtends + 1; j++)
            {
                for (int k = -bounds.zExtends; k < bounds.zExtends + 1; k++)
                {
                    int xIndex = i + bounds.xExtends;
                    int yIndex = j;
                    int zIndex = k + bounds.zExtends;

                    Vector3 cellOffsetFromCenter = new Vector3(i, j, k);
                    Vector3Int arrayIndex = new Vector3Int(xIndex, yIndex, zIndex);
                    Vector3 cellPosition = (cellOffsetFromCenter + bounds.position) * cellScale;
                    Vector3 samplePosition = (cellOffsetFromCenter * sampleOffset + bounds.position) * 
                                             (1.0f/sampleOffset);
                    
                    onLoop(arrayIndex, cellPosition, samplePosition);
                }
            }
        }
    }

    public static bool IsIndexWithinBoundsInDirection(Vector3Int index, Bounds bounds, Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                return index.y < bounds.yExtends + 1;
            case Direction.Down:
                return index.y >= 0;
            case Direction.Left:
                return index.x >= 0;
            case Direction.Right:
                return index.x < bounds.xExtends * 2 + 1;
            case Direction.Forwards:
                return index.z < bounds.zExtends * 2 + 1;
            case Direction.Backwards:
                return index.z >= 0;
            case Direction.Length:
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }
    
    public static Vector3Int DirectionEnumToVector(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                return new Vector3Int(0, 1, 0);
            case Direction.Down:
                return new Vector3Int(0, -1, 0);
            case Direction.Left:
                return new Vector3Int(-1, 0, 0);
            case Direction.Right:
                return new Vector3Int(1, 0, 0);
            case Direction.Forwards:
                return new Vector3Int(0, 0, 1);
            case Direction.Backwards:
                return new Vector3Int(0, 0, -1);
            case Direction.Length:
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }
}
