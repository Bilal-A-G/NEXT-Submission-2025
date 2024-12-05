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

        public bool touched = false;
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

        public void TryCollapse()
        {
            if (cellStates.Count != 1 || collapsed)
                return;
            
            collapsed = true;
            Display();
        }

        public void ForceCollapse()
        {
            if(collapsed || cellStates.Count <= 1)
                return;
            
            collapsed = true;
            int random = cellStates[Random.Range(0, cellStates.Count)];
            cellStates.Clear();
            cellStates.Add(random);
            Display();
        }

        private void Display()
        {
            if(cellStates.Count != 1)
                return;
            
            GameObject cellModel = Object.Instantiate(_constraint.GetPrefabAtIndex(cellStates[^1]), _displayParent);
            cellModel.transform.position = _position;
            cellModel.transform.localScale *= _cellSize; 
        }
    }

    public class LevelGenerator : MonoBehaviour
    {
        [Tooltip("The side length of a grid cell in meters")]
        [SerializeField] private float gridResolution;

        [SerializeField] private Bounds gridBounds;
        [SerializeField] private Constraint constraint;
        [SerializeField] private Transform levelParent;

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
            
            GenerateLevel();
        }

        //[ContextMenu("Generate Level")]
        private void GenerateLevel()
        {
            for (int i = 0; i < levelParent.childCount; i++)
            {
                Destroy(levelParent.GetChild(i).gameObject);
            }
            
            _cells = new Cell[gridBounds.xExtends * 2 + 1, gridBounds.yExtends + 1, gridBounds.zExtends * 2 + 1];
            Utility.LoopOverAllCells((Vector3Int arrayIndex, Vector3 cellPosition, Vector3 _) =>
            {
                //Creating an array, where each element has the same value as it's index
                List<int> allStates = new List<int>();

                for (int i = 0; i < constraint.GetPrefabCount(); i++)
                    allStates.Add(i);
                
                
                _cells[arrayIndex.x, arrayIndex.y, arrayIndex.z] = new Cell(cellPosition, allStates, 
                    gridResolution, constraint, levelParent);
            }, gridBounds, gridResolution);

            //Collapse until we run out of cells
            int lowestEntropyFound = CollapseRoutine();
            while (lowestEntropyFound < int.MaxValue)
            {
                lowestEntropyFound = CollapseRoutine();
            }
        }

        //Recursively propagates changes to a cell neighbour across the entire grid
        private void PropagateChanges(Vector3Int arrayIndex, Cell current)
        {
            current.TryCollapse();

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
                
                if(next.touched)
                    continue;

                next.touched = true;
                PropagateChanges(nextIndex, next);                                                                              
            }
        }

        //One iteration of the wave function collapse algorithm
        private int CollapseRoutine()
        {
            int lowestEntropy = int.MaxValue;
            Vector3Int lowestEntropyIndex = Vector3Int.zero;

            Utility.LoopOverAllCells((Vector3Int arrayIndex, Vector3 cellPosition, Vector3 _) =>
            {
                Cell current = _cells[arrayIndex.x, arrayIndex.y, arrayIndex.z];
                if (current.collapsed || current.cellStates.Count > lowestEntropy) 
                    return;

                //If current has same as lowest, then flip a coin to see if
                //we choose this or not
                int random = Random.Range(0, 2);
                if(current.cellStates.Count == lowestEntropy && random == 0)
                    return;
                
                lowestEntropy = current.cellStates.Count;
                lowestEntropyIndex = arrayIndex;
            }, gridBounds, gridResolution);

            Cell lowestEntropyCell = _cells[lowestEntropyIndex.x, lowestEntropyIndex.y, lowestEntropyIndex.z];
            lowestEntropyCell.ForceCollapse();
            PropagateChanges(lowestEntropyIndex, lowestEntropyCell);
            
            //Kinda brute force, would be nice to not loop
            Utility.LoopOverAllCells((Vector3Int arrayIndex, Vector3 cellPosition, Vector3 _) =>
            {
                _cells[arrayIndex.x, arrayIndex.y, arrayIndex.z].touched = false;
            }, gridBounds, gridResolution);

            return lowestEntropy;
        }

        private void OnDrawGizmos()
        {
            Utility.LoopOverAllCells((Vector3Int _, Vector3 cellPosition, Vector3 _) =>
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(cellPosition, Vector3.one * gridResolution);         
            }, gridBounds, gridResolution);
        }
    }
}