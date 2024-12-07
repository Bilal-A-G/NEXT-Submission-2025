using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModelSynthesis
{
    [System.Serializable]
//This stores all adjacent state indices
    public class AdjacencyConstraint
    {
        public List<int> up = new();
        public List<int> down = new();
        public List<int> left = new();
        public List<int> right = new();
        public List<int> forwards = new();
        public List<int> backwards = new();
    }

    [System.Serializable]
    public class State : IEquatable<State>
    {
        public int prefabIndex;
        public Vector3 rotation;
        public AdjacencyConstraint adjacencyConstraint;

        public State(int prefabIndex, Vector3 rotation)
        {
            this.prefabIndex = prefabIndex;
            this.rotation = rotation;
            adjacencyConstraint = new AdjacencyConstraint();
        }

        public bool Equals(State other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return prefabIndex == other.prefabIndex && rotation.Equals(other.rotation);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((State)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(prefabIndex, rotation);
        }
    }

    [CreateAssetMenu(menuName = "Level Generation", fileName = "New Profile")]
    public class Profile : ScriptableObject
    {
        [SerializeField] private List<GameObject> prefabs;
        [SerializeField] private List<State> allStates;
        [SerializeField] private int nullPrefabIndex;
        [HideInInspector] public Chunk[] chunks;

        public void ResetStates() => allStates = new List<State>();
    
        public int GetPrefabIndex(GameObject prefab)
        {
            for (int i = 0; i < prefabs.Count; i++)
            {
                if (prefabs[i] == prefab)
                    return i;
            }

            return nullPrefabIndex;
        }

        public int GetNullState()
        {
            for (int i = 0; i < allStates.Count; i++)
            {
                if (allStates[i].prefabIndex == nullPrefabIndex)
                    return i;
            }

            throw new System.ArgumentException("Error, there is no null prefab configured! " +
                                               "Please add one to the prefabs list and ensure null index is set to it's index");
        }

        public GameObject GetPrefabAtIndex(int index) => prefabs[index];
        public int GetStateCount() => allStates.Count;

        public GameObject GetPrefabAtStateIndex(int stateIndex) => GetPrefabAtIndex(allStates[stateIndex].prefabIndex);
        public Vector3 GetRotationAtStateIndex(int stateIndex) => allStates[stateIndex].rotation;
    
        public int GetStateIndex(int prefabIndex, Vector3 rotation)
        {
            State newState = new State(prefabIndex, rotation);
            for (int i = 0; i < allStates.Count; i++)
            {
                if (allStates[i].Equals(newState))
                    return i;
            }
        
            allStates.Add(newState);
            return allStates.Count - 1;
        }

        //Feed in index of state you want to add in toAdd
        public void AddAdjacencyToStateAtIndex(int toAdd, Direction direction, int stateIndex)
        {
            List<int> addingTo;
        
            switch (direction)
            {
                case Direction.Up:
                    addingTo = allStates[stateIndex].adjacencyConstraint.up;
                    break;
                case Direction.Down:
                    addingTo = allStates[stateIndex].adjacencyConstraint.down;
                    break;
                case Direction.Left:
                    addingTo = allStates[stateIndex].adjacencyConstraint.left;
                    break;
                case Direction.Right:
                    addingTo = allStates[stateIndex].adjacencyConstraint.right;
                    break;
                case Direction.Forwards:
                    addingTo = allStates[stateIndex].adjacencyConstraint.forwards;
                    break;
                case Direction.Backwards:
                    addingTo = allStates[stateIndex].adjacencyConstraint.backwards;
                    break;
                case Direction.Length:
                default:
                    addingTo = new List<int>();
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        
            if(!addingTo.Contains(toAdd))
                addingTo.Add(toAdd);
        }

        public List<int> GetStateIndicesAdjacentToStateIndex(Direction direction, int stateIndex)
        {
            switch (direction)
            {
                case Direction.Up:
                    return allStates[stateIndex].adjacencyConstraint.up;
                case Direction.Down:
                    return allStates[stateIndex].adjacencyConstraint.down;
                case Direction.Left:
                    return allStates[stateIndex].adjacencyConstraint.left;
                case Direction.Right:
                    return allStates[stateIndex].adjacencyConstraint.right;
                case Direction.Forwards:
                    return allStates[stateIndex].adjacencyConstraint.forwards;
                case Direction.Backwards:
                    return allStates[stateIndex].adjacencyConstraint.backwards;
                case Direction.Length:
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public List<State> GetStatesFromStateIndices(List<int> stateIndices)
        {
            List<State> toReturn = new List<State>();
            foreach (int index in stateIndices)
            {
                toReturn.Add(allStates[index]);
            }

            return toReturn;
        }
    }
}