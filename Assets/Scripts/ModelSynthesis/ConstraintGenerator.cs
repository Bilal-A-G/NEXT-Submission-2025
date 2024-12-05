using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

//This class bakes adjacency constraints from a user made level
//and stores it in a scriptable object,
//this object can then be used to procedurally generate a new level
public class ConstraintGenerator : MonoBehaviour
{
    [SerializeField] private Constraint constraint;
    [SerializeField] private Bounds bounds;
    [SerializeField] private float cellScale;
    [Range(1, 20)]
    [SerializeField] private float sampleOffset;
    [SerializeField] private string nullPrefabName;

    private GameObject[,,] _cells;

    [ContextMenu("Bake")]
    private void Bake()
    {
        constraint.ResetAdjacencies();

        _cells = new GameObject[bounds.xExtends * 2 + 1,
            bounds.yExtends + 1, bounds.zExtends * 2 + 1];

        Utility.LoopOverAllCells((Vector3Int arrayIndex, Vector3 cellPosition, Vector3 samplePosition) =>
        {
            Collider[] collisions = new Collider[1];
            Physics.OverlapBoxNonAlloc(samplePosition,
                Vector3.one * (1.0f / sampleOffset) / 2.0f, collisions);

            Transform collided = collisions[0] != null ? collisions[0].transform : null;
            if (collided == null)
                return;

            //Prefab utility is editor only, so we can only define constraints in editor,
            //which should be ok since we shouldn't be messing with the level generation in game
            PrefabInstanceStatus prefabStatus = PrefabUtility.GetPrefabInstanceStatus(collided.gameObject);
            if (prefabStatus != PrefabInstanceStatus.Connected) return;

            GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(collided.gameObject);
            if (prefab == null)
                return;

            //This is temp, in the future it will just assume the prefab has no parent
            prefab = prefab.transform.parent == null ? prefab : prefab.transform.parent.gameObject;

            Debug.Log("Detected : " + prefab.name);
            _cells[arrayIndex.x, arrayIndex.y, arrayIndex.z] = prefab;
        }, bounds, cellScale, sampleOffset);

        for (int i = 0; i < constraint.GetPrefabCount(); i++)
        {
            GameObject currentCell = constraint.GetPrefabAtIndex(i);

            //This is awful, we need a loop or something to get rid of the duped code
            Utility.LoopOverAllCells((Vector3Int arrayIndex, Vector3 cellPosition, Vector3 samplePosition) =>
            {
                GameObject cellContains = _cells[arrayIndex.x, arrayIndex.y, arrayIndex.z];
                if (cellContains != currentCell && (currentCell.name != nullPrefabName || cellContains != null)) 
                    return;
                
                int negX = arrayIndex.x - 1;
                int posX = arrayIndex.x + 1;

                int negY = arrayIndex.y - 1;
                int posY = arrayIndex.y + 1;

                int negZ = arrayIndex.z - 1;
                int posZ = arrayIndex.z + 1;

                GameObject left = negX >= 0 ? _cells[negX, arrayIndex.y, arrayIndex.z] : null;
                GameObject right = posX < bounds.xExtends * 2 + 1
                    ? _cells[posX, arrayIndex.y, arrayIndex.z]
                    : null;

                GameObject down = negY >= 0 ? _cells[arrayIndex.x, negY, arrayIndex.z] : null;
                GameObject up = posY < bounds.yExtends + 1
                    ? _cells[arrayIndex.x, posY, arrayIndex.z]
                    : null;

                GameObject backwards = negZ >= 0 ? _cells[arrayIndex.x, arrayIndex.y, negZ] : null;
                GameObject forwards = posZ < bounds.zExtends * 2 + 1
                    ? _cells[arrayIndex.x, arrayIndex.y, posZ]
                    : null;

                constraint.AddAdjacentIfNotContains(constraint.GetPrefabIndex(left), Direction.Left, i);
                constraint.AddAdjacentIfNotContains(constraint.GetPrefabIndex(right), Direction.Right, i);
                
                constraint.AddAdjacentIfNotContains(constraint.GetPrefabIndex(down), Direction.Down, i);
                constraint.AddAdjacentIfNotContains(constraint.GetPrefabIndex(up), Direction.Up, i);

                constraint.AddAdjacentIfNotContains(constraint.GetPrefabIndex(backwards), 
                    Direction.Backwards, i);
                constraint.AddAdjacentIfNotContains(constraint.GetPrefabIndex(forwards), 
                    Direction.Forwards, i);
            }, bounds, cellScale);
        }
        
        Debug.Log("Baked all adjacency data to scriptable object provided!");
    }

    //Grid visualization
    private void OnDrawGizmos()
    {
        Utility.LoopOverAllCells((Vector3Int _, Vector3 cellPosition, Vector3 samplePosition) =>
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(cellPosition, Vector3.one * cellScale);
                    
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(samplePosition, Vector3.one * (1.0f/sampleOffset));
        }, bounds, cellScale, sampleOffset);
    }
}
