using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ModelSynthesisConstraintGenerator : MonoBehaviour
{
    [SerializeField] private Constraints constraint;
    [SerializeField] private Bounds exampleBounds;
    [SerializeField] private float cellScale;
    [Range(1, 20)]
    [SerializeField] private float sampleOffset;

    private GameObject[,,] _cells;

    [ContextMenu("Bake")]
    private void Bake()
    {
        constraint.ResetAdjacencies();

        _cells = new GameObject[exampleBounds.xExtends * 2 + 1,
            exampleBounds.yExtends + 1, exampleBounds.zExtends * 2 + 1];

        Utility.LoopOverAllCells((Vector3 arrayIndex, Vector3 cellPosition, Vector3 samplePosition) =>
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
            _cells[(int)arrayIndex.x, (int)arrayIndex.y, (int)arrayIndex.z] = prefab;
        }, exampleBounds, cellScale, sampleOffset);

        for (int i = 0; i < constraint.cellStates.Count; i++)
        {
            GameObject currentCell = constraint.cellStates[i].prefab;

            //This is awful, we need a loop or something to get rid of the duped code
            Utility.LoopOverAllCells((Vector3 arrayIndex, Vector3 cellPosition, Vector3 samplePosition) =>
            {
                GameObject cellContains = _cells[(int)arrayIndex.x, (int)arrayIndex.y, (int)arrayIndex.z];
                if (currentCell != cellContains)
                    return;

                int negX = (int)arrayIndex.x - 1;
                int posX = (int)arrayIndex.x + 1;

                int negY = (int)arrayIndex.y - 1;
                int posY = (int)arrayIndex.y + 1;

                int negZ = (int)arrayIndex.z - 1;
                int posZ = (int)arrayIndex.z + 1;

                GameObject left = negX >= 0 ? _cells[negX, (int)arrayIndex.y, (int)arrayIndex.z] : null;
                GameObject right = posX < exampleBounds.xExtends * 2 + 1
                    ? _cells[posX, (int)arrayIndex.y, (int)arrayIndex.z]
                    : null;

                GameObject down = negY >= 0 ? _cells[(int)arrayIndex.x, negY, (int)arrayIndex.z] : null;
                GameObject up = posY < exampleBounds.yExtends + 1
                    ? _cells[(int)arrayIndex.x, posY, (int)arrayIndex.z]
                    : null;

                GameObject backwards = negZ >= 0 ? _cells[(int)arrayIndex.x, (int)arrayIndex.y, negZ] : null;
                GameObject forwards = posZ < exampleBounds.zExtends * 2 + 1
                    ? _cells[(int)arrayIndex.x, (int)arrayIndex.y, posZ]
                    : null;
                
                if(!constraint.adjacencyConstraints[i].left.Contains(left))
                    constraint.adjacencyConstraints[i].left.Add(left);
                if(!constraint.adjacencyConstraints[i].right.Contains(right))
                    constraint.adjacencyConstraints[i].right.Add(right);

                if(!constraint.adjacencyConstraints[i].down.Contains(down))
                    constraint.adjacencyConstraints[i].down.Add(down);
                if(!constraint.adjacencyConstraints[i].up.Contains(up))
                    constraint.adjacencyConstraints[i].up.Add(up);

                if(!constraint.adjacencyConstraints[i].backwards.Contains(backwards))
                    constraint.adjacencyConstraints[i].backwards.Add(backwards);
                if(!constraint.adjacencyConstraints[i].forwards.Contains(forwards))
                    constraint.adjacencyConstraints[i].forwards.Add(forwards);
                
            }, exampleBounds, cellScale);
        }
        
        Debug.Log("Baked all adjacency data to scriptable object provided!");
    }

    private void OnDrawGizmos()
    {
        Utility.LoopOverAllCells((Vector3 arrayIndex, Vector3 cellPosition, Vector3 samplePosition) =>
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(cellPosition, Vector3.one * cellScale);
                    
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(samplePosition, Vector3.one * (1.0f/sampleOffset));
        }, exampleBounds, cellScale, sampleOffset);
    }
}
