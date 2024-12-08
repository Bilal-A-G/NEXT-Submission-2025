using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace ModelSynthesis
{
    public class LevelGenerator : MonoBehaviour
    {
        [Tooltip("The side length of a grid cell in meters")]
        [SerializeField] private float gridResolution;

        [SerializeField] private Bounds gridBounds;
        [SerializeField] private Vector3Int gridPadding;
        [SerializeField] private Bounds chunkBounds;
        [Range(0, 1)] [SerializeField] private float gridTransparency;
        [Range(0, 1)] [SerializeField] private float chunkTransparency;
        [FormerlySerializedAs("constraint")] [SerializeField] private Profile profile;
        [SerializeField] private Transform levelParent;

        //TEMP, used as a failsafe during development to not crash the editor
        //But, it really shouldn't be necessary
        [Tooltip("The number of tries before the model synthesis algorithm gives up")]
        [SerializeField] private int maxSynthesisIterations;
        [Tooltip("The number of tries before the chunk generator gives up")]
        [SerializeField] private int maxChunkIterations;

        private Cell[,,] _cells;
        private InputSystem_Actions _inputSystem;
        private Bounds _paddedGrid;
        
        private void OnDisable()
        {
            _inputSystem.Disable();
        }

        private void Awake()
        {
            _inputSystem = new InputSystem_Actions();
            _inputSystem.Enable();
            
            _inputSystem.Player.Refresh.performed += ctx =>
            {
                GenerateLevel();
            };
        }

        private void Update()
        {
            GenerateLevel();
        }

        private bool GenerateChunk(int chunksX, int chunksY, int chunksZ, int i)
        {
            Vector3Int chunkOffset = new Vector3Int((i % chunksX),
                ((i % (chunksY * chunksX)) / chunksX),
                (i / (chunksX * chunksY)));

            List<(Vector3Int, Cell)> border = new List<(Vector3Int, Cell)>();
            
            Utility.LoopOverAllCells((Vector3Int arrayIndex, Vector3 cellPosition, Vector3 _) =>
            {
                //"UnCollapse" the wave function on the cells in our chunk
                Vector3Int gridSpaceIndex = arrayIndex + chunkOffset + gridPadding;
                Vector3 worldPosition = (new Vector3(gridSpaceIndex.x - _paddedGrid.xExtends, 
                    gridSpaceIndex.y, gridSpaceIndex.z - _paddedGrid.zExtends) + _paddedGrid.position) * gridResolution;
                
                Cell currentCell = _cells[gridSpaceIndex.x, gridSpaceIndex.y, gridSpaceIndex.z];
                
                currentCell.DeleteModel();
                //Initialize list with all possible states the cell can be in
                List<int> allStates = new List<int>();
                for (int j = 0; j < profile.GetStateCount(); j++)
                    allStates.Add(j);
                
                _cells[gridSpaceIndex.x, gridSpaceIndex.y, gridSpaceIndex.z] =
                    new Cell(worldPosition, allStates, gridResolution, profile, levelParent);
                
                //Do not proceed past this point if we're not a border index
                if (!Utility.IsIndexAtBounds(arrayIndex, chunkBounds, false)) 
                    return;
                
                for (int j = 0; j < (int)Direction.Length; j++)
                {
                    //If index is out of bounds of grid, then continue
                    Vector3Int directionVector = Utility.DirectionEnumToVector((Direction)j);
                    Vector3Int gridSpaceNeighbourIndex = gridSpaceIndex + directionVector;
                    
                    if(Utility.IsIndexOutOfBounds(gridSpaceNeighbourIndex, _paddedGrid))
                        continue;
                    
                    //If index is within the bounds of the grid, then continue
                    if(!Utility.IsIndexOutOfBounds(arrayIndex + directionVector, chunkBounds))
                        continue;
                    
                    //What we're left with is a chunk neighbour. A cell that borders the chunk without being in it
                    Cell neighbour = _cells[gridSpaceNeighbourIndex.x, 
                        gridSpaceNeighbourIndex.y, gridSpaceNeighbourIndex.z];
                        
                    border.Add((gridSpaceNeighbourIndex, neighbour));
                }
                
            }, chunkBounds, gridResolution);
            
            List<int> collapsed = new List<int>();
            
            for (int j = 0; j < border.Count; j++)
            {
                if(border[j].Item2.cellStates.Count > 1)
                    continue;
                
                PropagateChanges(border[j].Item1, border[j].Item2, ref collapsed, border[j].Item1);
            }
            
            int currentIteration = 0;
                
            //Collapse until we run out of cells
            bool stop = false;
            while (!stop && currentIteration < maxSynthesisIterations)
            {
                stop = CollapseRoutine(chunkBounds, chunkOffset, ref collapsed);
                currentIteration++;
            }
            
            int numCells = chunkBounds.GetWidth() * chunkBounds.GetHeight() * chunkBounds.GetDepth();
            
            //Return false if we fail
            return (currentIteration < maxSynthesisIterations) && (collapsed.Count >= numCells) && stop;
        }
        
        private void GenerateLevel()
        {
            for (int i = 0; i < levelParent.childCount; i++)
            {
                Destroy(levelParent.GetChild(i).gameObject);
            }

            int chunksX = gridBounds.GetWidth() - chunkBounds.GetWidth() + 1;
            int chunksY = gridBounds.GetHeight() - chunkBounds.GetHeight() + 1;
            int chunksZ = gridBounds.GetDepth() - chunkBounds.GetDepth() + 1;
            
            _cells = new Cell[_paddedGrid.GetWidth(), _paddedGrid.GetHeight(), _paddedGrid.GetDepth()];
            
            //Initialize all cells to be null
            Utility.LoopOverAllCells((Vector3Int arrayIndex, Vector3 cellPosition, Vector3 _) =>
            {
                _cells[arrayIndex.x, arrayIndex.y, arrayIndex.z] = new Cell(cellPosition, 
                    new List<int>(){profile.GetNullState()}, 
                    gridResolution, profile, levelParent);
                
                _cells[arrayIndex.x, arrayIndex.y, arrayIndex.z].TryCollapse();
            }, _paddedGrid, gridResolution);
            
            //This might seem silly, but I don't want to type out a 3d array, so I'm flattening and
            //reshaping1
            for (int i = 0; i < chunksX * chunksY * chunksZ; i++)
            {
                bool success = false;
                int loopIterations = 0;
                while (!success && loopIterations < maxChunkIterations)
                {
                    loopIterations++;
                    success = GenerateChunk(chunksX, chunksY, chunksZ, i);
                }
                
                Debug.Log("Terminated chunk generation at index " + i  + " at iteration " + loopIterations + 
                          " with success = " + success);
            }
        }

        //Recursively propagates changes to a cell neighbour across the entire grid
        private void PropagateChanges(Vector3Int arrayIndex, Cell current, ref List<int> collapsed, Vector3Int initiator)
        {
            if (current.TryCollapse())
                collapsed.Add(current.cellStates[^1]);
            
            for (int i = 0; i < (int)Direction.Length; i++)
            {
                Direction currentDirection = (Direction)i;
                Vector3Int nextIndex = arrayIndex + Utility.DirectionEnumToVector(currentDirection);
                
                if (Utility.IsIndexOutOfBounds(nextIndex, _paddedGrid))
                    continue;
                
                Cell next = _cells[nextIndex.x, nextIndex.y, nextIndex.z];
                
                List<int> intersection = new List<int>();
                for (int v = 0; v < current.cellStates.Count(); v++)
                {
                    List<int> adjacentIndices = profile.GetStateIndicesAdjacentToStateIndex(currentDirection, current.cellStates[v]);
                    List<int> stateIntersections = adjacentIndices.Intersect(next.cellStates).ToList();
                        
                    foreach (int stateIntersection in stateIntersections)
                    {
                        if(!intersection.Contains(stateIntersection))
                            intersection.Add(stateIntersection);
                    }
                }
                
                next.cellStates = intersection;
            }

            //Making sure we set changes first, then propagate
            for (int i = 0; i < (int)Direction.Length; i++)
            {
                Direction currentDirection = (Direction)i;
                Vector3Int nextIndex = arrayIndex + Utility.DirectionEnumToVector(currentDirection);

                if (Utility.IsIndexOutOfBounds(nextIndex, _paddedGrid))
                    continue;
                
                Cell next = _cells[nextIndex.x, nextIndex.y, nextIndex.z];
                
                if (next.lastTouchedByIndex == initiator || next.collapsed)
                    continue;
                
                next.lastTouchedByIndex = initiator;
                PropagateChanges(nextIndex, next, ref collapsed, initiator);
            }
        }

        //One iteration of the wave function collapse algorithm
        private bool CollapseRoutine(Bounds bounds, Vector3Int offset, ref List<int> collapsed)
        {
            List<Vector3Int> candidates = new List<Vector3Int>();
            
            Utility.LoopOverAllCells((Vector3Int arrayIndex, Vector3 cellPosition, Vector3 _) =>
            {
                Vector3Int cellArrayIndex = arrayIndex + offset + gridPadding;

                Cell current = _cells[cellArrayIndex.x, cellArrayIndex.y, cellArrayIndex.z];
                if (current.cellStates.Count < 1)
                    return;
                
                if (current.collapsed) 
                    return;
                
                candidates.Add(cellArrayIndex);
            }, bounds, gridResolution);
            
            //Failed to find an un-collapsed cell
            if (candidates.Count == 0)
                return true;

            Vector3Int cellIndex = candidates[Random.Range(0, candidates.Count)];
            
            Cell currentCell = _cells[cellIndex.x, cellIndex.y, cellIndex.z];
            if(currentCell.ForceCollapse(new List<int>(collapsed)))
                collapsed.Add(currentCell.cellStates[^1]);
            
            PropagateChanges(cellIndex, currentCell, ref collapsed, cellIndex);

            return false;
        }

        private void OnDrawGizmos()
        {
            _paddedGrid = new Bounds
            {
                xExtends = gridBounds.xExtends + gridPadding.x,
                yExtends = gridBounds.yExtends + gridPadding.y,
                zExtends = gridBounds.zExtends + gridPadding.z,
                position = gridBounds.position
            };
            
            Utility.LoopOverAllCells((Vector3Int _, Vector3 cellPosition, Vector3 _) =>
            {
                Gizmos.color = new Color(0, 255, 0, gridTransparency);
                Gizmos.DrawWireCube(cellPosition, Vector3.one * gridResolution);         
            }, _paddedGrid, gridResolution);

            Gizmos.color = new Color(0, 0, 255, chunkTransparency);
            Gizmos.DrawWireCube(gridBounds.position, 
                new Vector3(chunkBounds.GetWidth(), chunkBounds.GetHeight(), chunkBounds.GetDepth()) * gridResolution);
        }
    }
}