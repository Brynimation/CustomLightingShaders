using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
public static class MeshGenerator {

    //chunk size is in unity units. Resolution is the number of vertices in a row
    public static MeshData GenerateMesh(float[][] noiseMap, float heightMultiplier) 
    {
        Mesh mesh = new Mesh();
        int width = noiseMap.Length;
        int length = noiseMap[0].Length;
        Vector3[] verts = new Vector3[width * length];
        Vector2[] uvs = new Vector2[width * length];
        int[] tris = new int[(width - 1) * (length - 1) * 6];
        int index = 0;
        int triIndex = 0;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++) 
            {
                float xPos = -width/2f + (x /(float) width) * width;
                float zPos = -length / 2f + (z / (float)length) * length;
                float height = noiseMap[x][z] * heightMultiplier;
                verts[index] = new Vector3(xPos, height, zPos);
                uvs[index] = new Vector2(xPos / width, zPos / length);
                if (x < width - 1 && z < length - 1) 
                {
                    AddTriangle(index, index + width + 1, index + width, ref tris, ref triIndex);
                    AddTriangle(index, index + 1, index + width + 1, ref tris, ref triIndex);
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

