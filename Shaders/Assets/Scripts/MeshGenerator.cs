using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MeshData 
{
    public Mesh mesh;
    int triangleIndex;
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    public MeshData(Vector3[] vertices, Vector2[] uvs, int[] triangles) 
    {
        mesh = new Mesh();
        this.vertices = vertices;
        this.uvs = uvs;
        this.triangles = triangles;
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

}

public class NoiseMapData 
{
    public Color[] colourMap;
    public float[][] noiseMap;

    public NoiseMapData(Color[] colourMap, float[][] noiseMap) 
    {
        this.colourMap = colourMap;
        this.noiseMap = noiseMap;
    }
}

/*Struct to store the meshdata and noisemap data that will be added to our queues*/
public struct ThreadData<T> 
{
    public T data;
    public Action<T> callBack;

    public ThreadData(T data, Action<T> callback){
        this.data = data;
        this.callBack = callback;
    }
}
public static class MeshGenerator {

    public static NoiseMapData GenerateNoiseMapData(int chunkSize, float noiseScale) 
    {
        float[][] noiseMap = Noise.GenerateNoiseMap(chunkSize, noiseScale);
        int width = noiseMap.Length;
        int height = noiseMap[0].Length;
        Color[] pixels = new Color[width * height];
        
        //r.material.color = Color.red;
        Debug.Log(string.Format("{0},{1}", width, height));
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[(int)(y * width) + (int)x] = Color.Lerp(Color.black, Color.white, noiseMap[x][y]);
            }
        }
        return new NoiseMapData(pixels, noiseMap);
    }

    //chunk size is in unity units. Resolution is the number of vertices in a row
    public static MeshData GenerateMesh(float[][] noiseMap, float heightMultiplier, int currentLevelOfDetail) 
    {
        Mesh mesh = new Mesh();
        int width = noiseMap.Length;
        int length = noiseMap[0].Length;
        int meshSimplificationIncrement = (currentLevelOfDetail == 0) ? 1 : currentLevelOfDetail * 2;
        int vertsPerRow = (width - 1) / meshSimplificationIncrement + 1;
        int vertsPerCol = (length - 1) / meshSimplificationIncrement + 1;
        Vector3[] verts = new Vector3[width * length];
        Vector2[] uvs = new Vector2[width * length];
        int[] tris = new int[(width - 1) * (length - 1) * 6];
        int index = 0;
        int triIndex = 0;
        for (int x = 0; x < width; x+= meshSimplificationIncrement)
        {
            for (int z = 0; z < length; z+= meshSimplificationIncrement) 
            {
                float xPos = -width/2f + (x /(float) width) * width;
                float zPos = -length / 2f + (z / (float)length) * length;
                float height = noiseMap[x][z] * heightMultiplier;
                verts[index] = new Vector3(xPos, height, zPos);
                uvs[index] = new Vector2(xPos / width, zPos / length);
                if (x < width - 1 && z < length - 1) 
                {
                    AddTriangle(index, index + vertsPerRow + 1, index + vertsPerRow, ref tris, ref triIndex);
                    AddTriangle(index, index + 1, index + vertsPerRow + 1, ref tris, ref triIndex);
                    Debug.Log(triIndex);
                }
                index++;
            
            }
        }
        return new MeshData(verts, uvs, tris);
    }

    static void AddTriangle(int a, int b, int c, ref int[] tris, ref int triIndex) 
    {
        tris[triIndex] = a;
        tris[triIndex + 1] = b;
        tris[triIndex + 2] = c;
        triIndex += 3;
    }
}

