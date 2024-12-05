using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class Adjacencies
{
    //Positive y axis
    public List<int> up = new();
    //Negative y axis
    public List<int> down = new();
    //Negative x axis
    public List<int> left = new();
    //Positive x axis
    public List<int> right = new();
    //Positive z axis
    public List<int> forwards = new();
    //Negative z axis
    public List<int> backwards = new();
}

[CreateAssetMenu(menuName = "Constraints", fileName = "New Constraint")]
public class Constraint : ScriptableObject
{
    [SerializeField] private List<GameObject> prefabs;
    public List<Adjacencies> adjacencyConstraints;
    [SerializeField] private int nullPrefabIndex;

    public void ResetAdjacencies()
    {
        adjacencyConstraints = new List<Adjacencies>();
        for (int i = 0; i < prefabs.Count; i++)
        {
            adjacencyConstraints.Add(new Adjacencies());
        }
    }

    public int GetPrefabIndex(GameObject prefab)
    {
        for (int i = 0; i < prefabs.Count; i++)
        {
            if (prefabs[i] == prefab)
                return i;
        }

        return nullPrefabIndex;
    }

    public GameObject GetPrefabAtIndex(int index)
    {
        return prefabs[index];
    }

    public int GetPrefabCount()
    {
        return prefabs.Count;
    }

    public void AddAdjacentIfNotContains(int toAdd, Direction direction, int prefabIndex)
    {
        List<int> addingTo;

        switch (direction)
        {
            case Direction.Up:
                addingTo = adjacencyConstraints[prefabIndex].up;
                break;
            case Direction.Down:
                addingTo = adjacencyConstraints[prefabIndex].down;
                break;
            case Direction.Left:
                addingTo = adjacencyConstraints[prefabIndex].left;
                break;
            case Direction.Right:
                addingTo = adjacencyConstraints[prefabIndex].right;
                break;
            case Direction.Forwards:
                addingTo = adjacencyConstraints[prefabIndex].forwards;
                break;
            case Direction.Backwards:
                addingTo = adjacencyConstraints[prefabIndex].backwards;
                break;
            case Direction.Length:
            default:
                addingTo = new List<int>();
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
        
        if(!addingTo.Contains(toAdd))
            addingTo.Add(toAdd);
    }

    public List<int> GetAdjacencies(Direction direction, int prefabIndex)
    {
        switch (direction)
        {
            case Direction.Up:
                return adjacencyConstraints[prefabIndex].up;
            case Direction.Down:
                return adjacencyConstraints[prefabIndex].down;
            case Direction.Left:
                return adjacencyConstraints[prefabIndex].left;
            case Direction.Right:
                return adjacencyConstraints[prefabIndex].right;
            case Direction.Forwards:
                return adjacencyConstraints[prefabIndex].forwards;
            case Direction.Backwards:
                return adjacencyConstraints[prefabIndex].backwards;
            case Direction.Length:
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }
}
