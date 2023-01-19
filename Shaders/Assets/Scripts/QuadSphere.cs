using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plane 
{
    Mesh mesh;
    int resolution;
    Vector3[] verts;
    int[] tris;
    Vector2[] uvs;
    int triIndex;
    Vector3 localUp;
    Vector3 localRight;
    Vector3 localForward;
    public Plane(Mesh mesh, int resolution, Vector3 localUp) 
    {
        this.mesh = mesh;
        this.resolution = resolution;
        this.localUp = localUp;
        localRight = new Vector3(localUp.y, localUp.z, localUp.x);
        localForward = Vector3.Cross(localRight, localUp);
        
    }

    public void addTriangle(int a, int b, int c)
    {
        tris[triIndex] = a;
        tris[triIndex + 1] = b;
        tris[triIndex + 2] = c;
        triIndex += 3;

    }

    public void GenerateMesh() 
    {
        triIndex = 0;
        verts = new Vector3[resolution * resolution];
        uvs = new Vector2[resolution * resolution];
        //indices = new int[resolution * resolution];
        tris = new int[resolution * resolution * 6];
        int index = 0;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector2 percent = new Vector2(x, y)/(resolution - 1);
                Vector3 pointOnUnitCube = localUp + (percent.x - 0.5f) * 2 * localRight + (percent.y - 0.5f) * 2 *localForward;
                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;
                verts[index] = pointOnUnitSphere;
                

                if (x < resolution - 1 && y < resolution - 1)
                {
                    addTriangle(index, index + 1, index + resolution + 1); // triangles must be wound clockwise
                    addTriangle(index, index + resolution + 1, index + resolution);
                    
                }
                index++;

            }
        }
        mesh.Clear();
        mesh.vertices = verts;
        //mesh.SetUVs(0, uvs);
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        //mesh.SetIndices(indices, MeshTopology.Triangles, 0);
    }
}

public class QuadSphere : MonoBehaviour
{
    [SerializeField, HideInInspector]
    MeshFilter[] meshFilters;
    [SerializeField] Material mat;
    [SerializeField] int resolution;
    [SerializeField] float radius;
    Plane[] planes;

    void Start() 
    {
        Initialise();
        GenerateMesh();
    }
    private void OnValidate()
    {
        Initialise();
        GenerateMesh();
    }
    void Initialise()
    {
        if (meshFilters == null || meshFilters.Length == 0) 
        {
            meshFilters = new MeshFilter[6];
        }
        planes = new Plane[6];
        Vector3[] dirs = new Vector3[6]
        {Vector3.up, Vector3.down,
        Vector3.right, Vector3.left,
        Vector3.forward, Vector3.back
        };
        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i] == null) 
            {
                GameObject meshObject = new GameObject("mesh");
                meshObject.transform.SetParent(transform);
                meshObject.AddComponent<MeshRenderer>().sharedMaterial = mat;
                meshFilters[i] = meshObject.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = new Mesh();
                
            }
            planes[i] = new Plane(meshFilters[i].sharedMesh, resolution, dirs[i]);

        }
    }

    public void GenerateMesh() 
    {
        foreach (Plane plane in planes) 
        {
            plane.GenerateMesh();
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
