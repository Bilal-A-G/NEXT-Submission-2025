const float PI = 3.141592653589793238462643383279502884197169;

float Remap(float value, float low, float high, float newLow, float newHigh)
{
    return newLow + (value - low) * (newHigh - newLow) / (high - low);
}

float HenyeyGreenstein(float dotAngle, float g)
{
    return 1.0f/(4.0f * PI) * ((1.0f - pow(abs(g), 2.0f)) /
        pow(abs(1.0f + pow(abs(g), 2.0f) - g * 2.0f * cos(dotAngle)), 3.0f/2.0f));
}

float ShapeAlteringHeight(float percentHeight, float maxHeightPercent)
{
    float bottomRounding = clamp(Remap(percentHeight, 0.0f, 0.07f, 0.0f, 1.0f), 0.0f, 1.0f);
    float topRounding = clamp(Remap(percentHeight, maxHeightPercent * 0.1f, 
            maxHeightPercent, 1, 0), 0.0f, 1.0f);

    return bottomRounding * topRounding;
}

float DensityAlteringHeight(float percentHeight, float localDensity, float globalDensity)
{
    float bottomDensity = percentHeight * clamp(Remap(percentHeight, 0,
        0.15f, 0, 1),0.0f, 1.0f);
    float topDensity = clamp(Remap(percentHeight, 0.9f, 1, 1, 0), 0.0f, 1.0f);

    return globalDensity * bottomDensity * topDensity * localDensity * 2;
}