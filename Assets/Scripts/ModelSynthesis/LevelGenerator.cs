using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ModelSynthesis
{
    public class Cell
    {
        private Vector3 _position;
        public List<int> cellStates;
        private float _cellSize;
        private Constraint _constraint;
        private Transform _displayParent;

        private GameObject _visualRepresentation;

        public Vector3Int lastTouchedByIndex = new Vector3Int(-1, -1, -1);
        public bool collapsed = false;

        public Cell(Vector3 position, List<int> cellStates, float cellSize, 
            Constraint constraint, Transform displayParent)
        {
            _position = position;
            _cellSize = cellSize;
            _constraint = constraint;
            _displayParent = displayParent;

            this.cellStates = cellStates;
        }

        public bool TryCollapse()
        {
            if (cellStates.Count != 1 || collapsed)
                return false;

            collapsed = true;
            
            Display();
            return true;
        }

        public bool ForceCollapse()
        {
            if(collapsed || cellStates.Count <= 1)
                return false;
            
            collapsed = true;
            int random = cellStates[Random.Range(0, cellStates.Count)];
            cellStates.Clear();
            cellStates.Add(random);
            Display();

            return true;
        }

        private void Display()
        {
            GameObject cellModel = Object.Instantiate(_constraint.GetPrefabAtIndex(cellStates[^1]), _displayParent);
            cellModel.transform.position = _position;
            cellModel.transform.localScale *= _cellSize;
            _visualRepresentation = cellModel;
        }

        public void DeleteModel() => Object.Destroy(_visualRepresentation);
        
    }

    public class LevelGenerator : MonoBehaviour
    {
        [Tooltip("The side length of a grid cell in meters")]
        [SerializeField] private float gridResolution;

        [SerializeField] private Bounds gridBounds;
        [SerializeField] private Bounds chunkBounds;
        [Range(0, 1)] [SerializeField] private float gridTransparency;
        [Range(0, 1)] [SerializeField] private float chunkTransparency;
        [SerializeField] private Constraint constraint;
        [SerializeField] private Transform levelParent;

        //TEMP, used as a failsafe during development to not crash the editor
        //But, it really shouldn't be necessary
        [SerializeField] private int maxIterations;

        private Cell[,,] _cells;
        private InputSystem_Actions _inputSystem;
        
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
             //GenerateLevel();
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
                Vector3Int gridSpaceIndex = arrayIndex + chunkOffset;
                Vector3 worldPosition = (new Vector3(gridSpaceIndex.x - gridBounds.xExtends, 
                    gridSpaceIndex.y, gridSpaceIndex.z - gridBounds.zExtends) + gridBounds.position) * gridResolution;
                
                List<int> allStates = new List<int>();
                Cell currentCell = _cells[gridSpaceIndex.x, gridSpaceIndex.y, gridSpaceIndex.z];
                
                currentCell.DeleteModel();
                
                for (int j = 0; j < constraint.GetPrefabCount(); j++)
                    allStates.Add(j);
                
                _cells[gridSpaceIndex.x, gridSpaceIndex.y, gridSpaceIndex.z] =
                    new Cell(worldPosition, allStates, gridResolution, constraint, levelParent);
                
                //Do not proceed past this point if we're not a border index
                if (!Utility.isIndexAtBounds(arrayIndex, chunkBounds)) 
                    return;
                
                for (int j = 0; j < (int)Direction.Length; j++)
                {
                    //If index is out of bounds of grid, then continue
                    Vector3Int directionVector = Utility.DirectionEnumToVector((Direction)j);
                    Vector3Int gridSpaceNeighbourIndex = gridSpaceIndex + directionVector;
                    if(!Utility.IsIndexWithinBoundsInDirection(gridSpaceNeighbourIndex, gridBounds, (Direction)j))
                        continue;

                    //If index is within bounds of the chunk then continue
                    Vector3Int chunkSpaceNeighbourIndex = arrayIndex + directionVector;
                    if(!Utility.IsIndexOutOfBounds(chunkSpaceNeighbourIndex, chunkBounds))
                        continue;
                        
                    //What we're left with is a chunk neighbour. A cell that borders the chunk without being in it
                    Cell neighbour = _cells[gridSpaceNeighbourIndex.x, 
                        gridSpaceNeighbourIndex.y, gridSpaceNeighbourIndex.z];
                        
                    border.Add((gridSpaceNeighbourIndex, neighbour));
                }
                
            }, chunkBounds, gridResolution);

            int collapsed = 0;
            for (int j = 0; j < border.Count; j++)
            {
                PropagateChanges(border[j].Item1, border[j].Item2, ref collapsed, border[j].Item1);
            }
            
            int currentIteration = 0;
                
            //Collapse until we run out of cells
            bool stop = false;
            int numCollapsed = collapsed;
            while (!stop && currentIteration < maxIterations)
            {
                stop = CollapseRoutine(chunkBounds, chunkOffset, ref numCollapsed);
                currentIteration++;
            }

            //Regenerate false if we fail
            return (currentIteration < maxIterations) && (numCollapsed >= chunksX * chunksY * chunksZ) && stop;
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
            
            _cells = new Cell[gridBounds.xExtends * 2 + 1, gridBounds.yExtends + 1, gridBounds.zExtends * 2 + 1];
            
            //Initialize all cells to be null
            Utility.LoopOverAllCells((Vector3Int arrayIndex, Vector3 cellPosition, Vector3 _) =>
            {
                _cells[arrayIndex.x, arrayIndex.y, arrayIndex.z] = new Cell(cellPosition, 
                    new List<int>(){constraint.GetPrefabIndex(null)}, 
                    gridResolution, constraint, levelParent);
                
                _cells[arrayIndex.x, arrayIndex.y, arrayIndex.z].TryCollapse();
            }, gridBounds, gridResolution);
            
            //This might seem silly, but I don't want to type out a 3d array, so I'm flattening and
            //reshaping
            for (int i = 0; i < chunksX * chunksY * chunksZ; i++)
            {
                bool success = GenerateChunk(chunksX, chunksY, chunksZ, i);
                int breakAt = 0;
                for (int j = 0; j < 1000; j++)
                {
                    breakAt++;
                    
                    if (success)
                        break;
                    
                    success = GenerateChunk(chunksX, chunksY, chunksZ, i);
                }
                
                Debug.Log("Terminated chunk generation at iteration " + breakAt);
                Debug.Log("Chunk generation success = " + success);
            }
        }

        //Recursively propagates changes to a cell neighbour across the entire grid
        private void PropagateChanges(Vector3Int arrayIndex, Cell current, ref int collapsed, Vector3Int initiator)
        {
            if (current.TryCollapse())
                collapsed++;

            for (int i = 0; i < (int)Direction.Length; i++)
            {
                Direction currentDirection = (Direction)i;
                Vector3Int nextIndex = arrayIndex + Utility.DirectionEnumToVector(currentDirection);
                
                if (!Utility.IsIndexWithinBoundsInDirection(nextIndex, gridBounds, currentDirection))
                    continue;
                Cell next = _cells[nextIndex.x, nextIndex.y, nextIndex.z];
                
                List<int> intersection = new List<int>();
                for (int v = 0; v < current.cellStates.Count(); v++)
                {
                    List<int> adjacencies = constraint.GetAdjacencies(currentDirection, current.cellStates[v]);
                    List<int> stateIntersections = adjacencies.Intersect(next.cellStates).ToList();
                    foreach (var stateIntersection in stateIntersections)
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
                
                if (!Utility.IsIndexWithinBoundsInDirection(nextIndex, gridBounds, currentDirection))
                    continue;
                Cell next = _cells[nextIndex.x, nextIndex.y, nextIndex.z];
                
                if(next.lastTouchedByIndex != initiator)
                    continue;

                next.lastTouchedByIndex = initiator;
                PropagateChanges(nextIndex, next, ref collapsed, initiator);                                                                              
            }
        }

        //One iteration of the wave function collapse algorithm
        private bool CollapseRoutine(Bounds bounds, Vector3Int offset, ref int collapsed)
        {
            Vector3Int cellIndex = new Vector3Int(-1, 0, 0);
            bool found = false;
            
            Utility.LoopOverAllCells((Vector3Int arrayIndex, Vector3 cellPosition, Vector3 _) =>
            {
                Vector3Int cellArrayIndex = arrayIndex + offset;

                Cell current = _cells[cellArrayIndex.x, cellArrayIndex.y, cellArrayIndex.z];
                if (current.cellStates.Count < 1)
                    return;
                
                if (current.collapsed || found) 
                    return;

                cellIndex = cellArrayIndex;
                found = true;
            }, bounds, gridResolution);
            
            //Failed to find an un-collapsed cell
            if (!found)
                return true;
            
            Cell currentCell = _cells[cellIndex.x, cellIndex.y, cellIndex.z];
            if(currentCell.ForceCollapse())
                collapsed++;
            PropagateChanges(cellIndex, currentCell, ref collapsed, cellIndex);

            return false;
        }

        private void OnDrawGizmos()
        {
            Utility.LoopOverAllCells((Vector3Int _, Vector3 cellPosition, Vector3 _) =>
            {
                Gizmos.color = new Color(0, 255, 0, gridTransparency);
                Gizmos.DrawWireCube(cellPosition, Vector3.one * gridResolution);         
            }, gridBounds, gridResolution);

            Gizmos.color = new Color(0, 0, 255, chunkTransparency);
            Gizmos.DrawWireCube(gridBounds.position, 
                new Vector3(chunkBounds.GetWidth(), chunkBounds.GetHeight(), chunkBounds.GetDepth()) * gridResolution);
        }
    }
}