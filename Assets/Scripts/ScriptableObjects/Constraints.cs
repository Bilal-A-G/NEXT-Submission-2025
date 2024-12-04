using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class Adjacencies
{
    //Positive y axis
    public List<GameObject> up = new();
    //Negative y axis
    public List<GameObject> down = new();
    //Negative x axis
    public List<GameObject> left = new();
    //Positive x axis
    public List<GameObject> right = new();
    //Positive z axis
    public List<GameObject> forwards = new();
    //Negative z axis
    public List<GameObject> backwards = new();
}

[System.Serializable]
public struct CellState
{
    //Weights are relative, ie, if there's 2 states, 1 with a weight of 1, other with a weight of 0.5,
    //The 1 weighted one is 2 times more likely to spawn, it doesn't mean it's spawn chance is 100%
    [Range(0, 1)] public float weight;
    public GameObject prefab;
}

[CreateAssetMenu(menuName = "Constraints", fileName = "New Constraint")]
public class Constraints : ScriptableObject
{
    public List<CellState> cellStates;
    public List<Adjacencies> adjacencyConstraints;

    public void ResetAdjacencies()
    {
        adjacencyConstraints = new List<Adjacencies>();
        for (int i = 0; i < cellStates.Count; i++)
        {
            adjacencyConstraints.Add(new Adjacencies());
        }
    }
}
