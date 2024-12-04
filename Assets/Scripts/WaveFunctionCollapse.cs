using System;
using UnityEngine;

public struct WFCCell
{
    public Vector3 position;

    public WFCCell(Vector3 position)
    {
        this.position = position;
    }
}

public class WaveFunctionCollapse : MonoBehaviour
{
    [Tooltip("The side length of a grid cell in meters")]
    [SerializeField] private float gridResolution;

    [SerializeField] private Bounds gridBounds;

    private WFCCell[,,] _cells;
    
    private void InitializeGrid()
    {
        _cells = new WFCCell[gridBounds.xExtends * 2 + 1, gridBounds.yExtends + 1, gridBounds.zExtends * 2 + 1];
        Utility.LoopOverAllCells((Vector3 arrayIndex, Vector3 cellPosition, Vector3 _) =>
        {
            _cells[(int)arrayIndex.x, (int)arrayIndex.y, (int)arrayIndex.z] = new WFCCell(cellPosition);
        }, gridBounds, gridResolution);
    }

    private void OnDrawGizmos()
    {
        InitializeGrid();
        
        Utility.LoopOverAllCells((Vector3 arrayIndex, Vector3 cellPosition, Vector3 _) =>
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(cellPosition, Vector3.one * gridResolution);         
        }, gridBounds, gridResolution);
    }
}
