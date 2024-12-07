using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace ModelSynthesis
{
    //This class bakes adjacency constraints from a user made level
    //and stores it in a scriptable object,
    //this object can then be used to procedurally generate a new level
    public class ProfileGenerator : MonoBehaviour
    {
        [SerializeField] private Profile profile;
        [SerializeField] private Bounds bounds;
        [SerializeField] private float cellScale;
        [Range(1, 20)]
        [SerializeField] private float sampleOffset;
        [Range(0, 1)] [SerializeField] private float gridTransparency;

        [SerializeField] private Bounds chunkBounds;
        [Range(0, 1)] [SerializeField] private float chunkTransparency;

        private (GameObject, Vector3 rotation)[,,] _cells;
        private Chunk[] _chunks;

        [ContextMenu("Bake")]
        private void Bake()
        {
            profile.ResetStates();

            _cells = new (GameObject, Vector3)[bounds.GetWidth(), bounds.GetHeight(), bounds.GetDepth()];
            
            Utility.LoopOverAllCells((Vector3Int arrayIndex, Vector3 _, Vector3 samplePosition) =>
            {
                Collider[] collisions = new Collider[1];
                Physics.OverlapBoxNonAlloc(samplePosition,
                    Vector3.one * (1.0f / sampleOffset) / 2.0f, collisions);

                Transform collided = collisions[0] != null ? collisions[0].transform : null;
                if (collided == null)
                    return;

                if (collided.parent != null)
                    collided = collided.parent;
                
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
                _cells[arrayIndex.x, arrayIndex.y, arrayIndex.z] = (prefab, collided.rotation.eulerAngles);
            }, bounds, cellScale, sampleOffset);

            Utility.LoopOverAllCells((Vector3Int arrayIndex, Vector3 _, Vector3 _) =>
            {
                (GameObject, Vector3) cellContains = _cells[arrayIndex.x, arrayIndex.y, arrayIndex.z];
                int stateIndex = profile.GetStateIndex(profile.GetPrefabIndex(cellContains.Item1), cellContains.Item2);

                for (int i = 0; i < (int)Direction.Length; i++)
                {
                    Direction currentDirection = (Direction)i;
                    
                    Vector3Int neighbourIndex = Utility.DirectionEnumToVector(currentDirection) + arrayIndex;
                    if(Utility.IsIndexOutOfBounds(neighbourIndex, bounds))
                        continue;

                    (GameObject, Vector3) tuple = _cells[neighbourIndex.x, neighbourIndex.y, neighbourIndex.z];
                    int neighbourStateIndex = profile.GetStateIndex(profile.GetPrefabIndex(tuple.Item1), 
                        tuple.Item1 != null ? tuple.Item2 : Vector3.zero);
                    
                    profile.AddAdjacencyToStateAtIndex(neighbourStateIndex, currentDirection, stateIndex);
                }
            }, bounds, cellScale);
            
            Debug.Log("Generating chunks");
            
            //Stride of the chunk is assumed to be the w/h/d, TODO, add checking to make sure you
            //can't specify a bounds and chunk bounds that are incompatible
            int chunksX = (bounds.GetWidth() - chunkBounds.GetWidth())/chunkBounds.GetWidth() + 1;
            int chunksY = (bounds.GetHeight() - chunkBounds.GetHeight())/chunkBounds.GetHeight() + 1;
            int chunksZ = (bounds.GetDepth() - chunkBounds.GetDepth())/chunkBounds.GetDepth() + 1;

            _chunks = new Chunk[chunksX * chunksY * chunksZ];

            for (int i = 0; i < _chunks.Length; i++)
            {
                Chunk currentChunk = new Chunk();
                Vector3Int chunkOffset = new Vector3Int((i % chunksX) * chunkBounds.GetWidth(),
                    ((i % (chunksY * chunksX)) / chunksX) * chunkBounds.GetHeight(),
                    (i / (chunksX * chunksY)) * chunkBounds.GetDepth());
                
                Utility.LoopOverAllCells((Vector3Int arrayIndex, Vector3 _, Vector3 _) =>
                {
                    Vector3Int cellArrayIndex = arrayIndex + chunkOffset;
                    (GameObject, Vector3) tuple = _cells[cellArrayIndex.x, cellArrayIndex.y, cellArrayIndex.z];
                    int stateIndex = profile.GetStateIndex(profile.GetPrefabIndex(tuple.Item1), 
                        tuple.Item1 != null ? tuple.Item2 : Vector3.zero);
                    
                    currentChunk.cells.Add(stateIndex);
                }, chunkBounds, cellScale, sampleOffset);

                _chunks[i] = currentChunk;
            }

            profile.chunks = _chunks;
        
            Debug.Log("Baked all adjacency and chunk data to scriptable object provided!");
        }

        //Grid visualization
        private void OnDrawGizmos()
        {
            Utility.LoopOverAllCells((Vector3Int _, Vector3 cellPosition, Vector3 samplePosition) =>
            {
                Gizmos.color = new Color(0, 255, 0, gridTransparency);
                Gizmos.DrawWireCube(cellPosition, Vector3.one * cellScale);
                    
                Gizmos.color = new Color(255, 0, 0, gridTransparency);
                Gizmos.DrawWireCube(samplePosition, Vector3.one * (1.0f/sampleOffset));
            }, bounds, cellScale, sampleOffset);
        
            Gizmos.color = new Color(0, 0, 255, chunkTransparency);
            Gizmos.DrawWireCube(bounds.position, new Vector3(chunkBounds.xExtends * 2 + 1, 
                chunkBounds.yExtends + 1, chunkBounds.zExtends * 2 + 1));
        }
    }
}