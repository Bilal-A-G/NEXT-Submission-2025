using UnityEngine;

namespace Utility
{
    public static class Math
    {
        public static float Remap(float value, float low, float high, float newLow, float newHigh)
        {
            return newLow + (value - low) * (newHigh - newLow) / (high - low);
        }

        //Max height percent comes from the weather map
        //Percent height is the height percentage of where the currently sampled value is in the cloud
        //I assume Remap(height, 0, maxHeight, 0, 1)?
        public static float ShapeAlteringHeight(float percentHeight, float maxHeightPercent)
        {
            float bottomRounding = Mathf.Clamp01(Remap(percentHeight, 0, 0.07f, 0, 1));
            float topRounding = Mathf.Clamp01(Remap(percentHeight, maxHeightPercent * 0.2f, 
                percentHeight, 1, 0));

            return bottomRounding * topRounding;
        }
    
        //Local density comes from the weather map
        public static float DensityAlteringHeight(float percentHeight, float localDensity, float globalDensity)
        {
            float bottomDensity = percentHeight * Mathf.Clamp01(Remap(percentHeight, 0, 0.15f, 0, 1));
            float topDensity = Mathf.Clamp01(Remap(percentHeight, 0.9f, 1, 1, 0));

            return globalDensity * bottomDensity * topDensity * localDensity * 2;
        }
    }
}
