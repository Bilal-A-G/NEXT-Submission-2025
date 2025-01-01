using System;
using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsCounter;
    [SerializeField] private int sampleOverFrames;

    private float _currentAverageFPS;
    private int _currentFrame;
    void Update()
    {        
        _currentFrame++;
        _currentAverageFPS += (Time.deltaTime * 1000.0f);

        if (_currentFrame != sampleOverFrames) return;
        
        _currentAverageFPS /= sampleOverFrames;
        _currentFrame = 0;
        fpsCounter.text = "FPS : " + Mathf.RoundToInt(1000.0f / _currentAverageFPS);
    }
}
