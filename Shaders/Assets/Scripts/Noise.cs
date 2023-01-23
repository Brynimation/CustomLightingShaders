using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class Noise
{
    /*Generate a noisemap - a grid of values between 0 and 1*/

    public static float[][] GenerateNoiseMap(int textureSize, float noiseScale) 
    {
        float[][] noiseMap = new float[textureSize][];
        noiseScale = Mathf.Clamp(noiseScale, 0.001f, 1f);
        Debug.Log(textureSize);
        for (int x = 0; x < textureSize; x++) 
        {
            float[] noiseMapCol = new float[textureSize];
            for(int y =0; y < textureSize; y++) 
            {
                noiseMapCol[y] = Mathf.PerlinNoise(x /(float) noiseScale, y / (float)noiseScale);
            }
            noiseMap[x] = noiseMapCol;
        }
        Debug.Log(noiseMap.Length);
        return noiseMap;
    } 
}
