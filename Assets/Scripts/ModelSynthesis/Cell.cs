using System.Collections.Generic;
using UnityEngine;

namespace ModelSynthesis
{
    public class Cell
    {
        private Vector3 _position;
        public List<int> cellStates;
        private float _cellSize;
        private Profile _profile;
        private Transform _displayParent;

        private GameObject _visualRepresentation;

        public Vector3Int lastTouchedByIndex = new Vector3Int(-100, -100, -100);
        public bool collapsed = false;

        public Cell(Vector3 position, List<int> cellStates, float cellSize, 
            Profile profile, Transform displayParent)
        {
            _position = position;
            _cellSize = cellSize;
            _profile = profile;
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

        private int PickOptimalState(List<int> currentModel)
        {
            float[] stateProbabilities = new float[cellStates.Count];
            float[] cumulativeProbabilities = new float[cellStates.Count];
            int total = 0;
            
            for (int i = 0; i < stateProbabilities.Length; i++)
            {
                currentModel.Add(cellStates[i]);
                for (int j = 0; j < _profile.chunks.Length; j++)
                {
                    int perceptualDistance = Utility.PerceptualDistance(currentModel, _profile.chunks[j].cells);
                    if(perceptualDistance < _profile.perceptualDistanceThreshold)
                        continue;
                    
                    stateProbabilities[i]++;
                    total++;
                }
                currentModel.RemoveAt(currentModel.Count - 1);
            }

            float cumulativeSum = 0;
            for (int i = 0; i < stateProbabilities.Length; i++)
            {
                stateProbabilities[i] /= total;
                cumulativeSum += stateProbabilities[i];
                cumulativeProbabilities[i] = cumulativeSum;
            }

            float random = Random.Range(0.0f, 1.0f);
            int collapsedState = _profile.GetNullState();
            
            for (int i = 0; i < stateProbabilities.Length; i++)
            {
                if (!(random <= cumulativeProbabilities[i])) continue;
                
                collapsedState = cellStates[i];
                break;
            }
            
            return collapsedState;
        }

        public bool ForceCollapse(List<int> currentModel)
        {
            if(collapsed || cellStates.Count <= 1)
                return false;
            
            collapsed = true;
            int pickedState = PickOptimalState(currentModel);
            cellStates.Clear();
            cellStates.Add(pickedState);
            Display();

            return true;
        }

        private void Display()
        {
            GameObject cellModel = Object.Instantiate(_profile.GetPrefabAtStateIndex(cellStates[^1]), _displayParent);
            cellModel.transform.position = _position;
            cellModel.transform.localScale *= _cellSize;
            cellModel.transform.eulerAngles = _profile.GetRotationAtStateIndex(cellStates[^1]);
            _visualRepresentation = cellModel;
        }

        public void DeleteModel() => Object.Destroy(_visualRepresentation);
    }
}
