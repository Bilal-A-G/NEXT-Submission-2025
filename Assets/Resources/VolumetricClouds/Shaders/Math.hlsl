float R(float value, float low, float high, float newLow, float newHigh)
{
    return newLow + (value - low) * (newHigh - newLow) / (high - low);
}

//SLerp implementation from: https://www.shadertoy.com/view/4sV3zt
//Interpolates 2 direction vectors in a spherical way.
//Ie, rotates a vector by a percentage until it faces the other vector
float3 SLerp(float3 start, float3 end, float percent)
{
    // Dot product - the cosine of the angle between 2 vectors.
    float startEndDot = dot(start, end);     
    // Clamp it to be in the range of Acos()
    // This may be unnecessary, but floating point
    // precision can be a fickle mistress.
    startEndDot = clamp(startEndDot, -1.0, 1.0);
    // Acos(dot) returns the angle between start and end,
    // And multiplying that by percent returns the angle between
    // start and the final result.
    float theta = acos(startEndDot)*percent;
    float3 RelativeVec = normalize(end - start*startEndDot); // Orthonormal basis
    // The final result.
    return ((start*cos(theta)) + (RelativeVec*sin(theta)));
}

float2 RaySphereIntersect(float3 rayOrigin, float3 rayDirection, float3 spherePosition, float sphereRadius)
{
    float3 toSphere = rayOrigin - spherePosition;
    
    float b = 2 * dot(rayDirection, toSphere);
    float c = pow(length(toSphere), 2) - pow(sphereRadius, 2);
    float discriminant = pow(b, 2) - 4 * c;

    if(discriminant < 0)
        return float2(-1, -1);
        
    float farIntersection = -b/2.0f + sqrt(discriminant)/2.0f;
    float nearIntersection = -b/2.0f - sqrt(discriminant)/2.0f;

    return float2(nearIntersection, farIntersection);
}

float ShapeAlteringHeight(float percentHeight, float maxHeightPercent)
{
    float bottomRounding = clamp(R(percentHeight, 0.0f, 0.07f, 0.0f, 1.0f), 0.0f, 1.0f);
    float topRounding = clamp(R(percentHeight, maxHeightPercent * 0.1f, 
            maxHeightPercent, 1, 0), 0.0f, 1.0f);

    return bottomRounding * topRounding;
}

float DensityAlteringHeight(float percentHeight, float localDensity, float globalDensity)
{
    float bottomDensity = percentHeight * clamp(R(percentHeight, 0,
        0.15f, 0, 1),0.0f, 1.0f);
    float topDensity = clamp(R(percentHeight, 0.9f, 1, 1, 0), 0.0f, 1.0f);

    return globalDensity * bottomDensity * topDensity * localDensity * 2;
}