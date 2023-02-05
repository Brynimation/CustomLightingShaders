using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TerrainChunkQuadTree 
{
    public Bounds bounds;
    Transform parent;
    public int minChunkSize = 1;
    public float heightMapMultiplier = 10f;
    public float distanceThreshold;
    public int LOD;
    Vector3 centre;
    int chunkSize;
    bool divided;
    TerrainChunkQuadTree BL;
    TerrainChunkQuadTree BR;
    TerrainChunkQuadTree TL;
    TerrainChunkQuadTree TR;


    public TerrainChunkQuadTree(Vector3 centre, int chunkSize, int LOD, float heightMapMultiplier, Transform parent, float distanceThreshold) 
    {
        this.parent = parent;
        this.distanceThreshold = distanceThreshold;
        this.LOD = LOD;
        this.heightMapMultiplier = heightMapMultiplier;
            this.chunkSize = chunkSize;
        this.centre = centre;
        bounds = new Bounds(centre, new Vector3(chunkSize, 1f, chunkSize));
        GenerateChunk();
    }

    public void GenerateChunk() 
    {
        GameObject chunk = new GameObject("Chunk");
        chunk.transform.SetParent(parent);
        
        MeshFilter mf = chunk.AddComponent<MeshFilter>();
        MeshRenderer mr = chunk.AddComponent<MeshRenderer>();   
        NoiseMapData noiseMapData = MeshGenerator.GenerateNoiseMapData(chunkSize, 0.3f);
        MeshData meshData = MeshGenerator.GenerateMesh(noiseMapData.noiseMap, heightMapMultiplier, LOD);
        Texture2D texture = new Texture2D(chunkSize, chunkSize);
        mf.mesh = meshData.getMesh();
        mr.material.mainTexture = texture;
        texture.SetPixels(noiseMapData.colourMap);
        texture.Apply();
        chunk.transform.position = centre;
    }

    public void Subdivide(Vector3 BLCentre, Vector3 BRCentre, Vector3 TLCentre, Vector3 TRCentre) 
    {
        BL = new TerrainChunkQuadTree(BLCentre, chunkSize / 2, LOD/2, heightMapMultiplier, parent, distanceThreshold/2f);
        BR = new TerrainChunkQuadTree(BRCentre, chunkSize / 2, LOD/2, heightMapMultiplier,parent, distanceThreshold / 2f);
        TL = new TerrainChunkQuadTree(TLCentre, chunkSize / 2, LOD/2, heightMapMultiplier, parent, distanceThreshold / 2f);
        TR = new TerrainChunkQuadTree(TRCentre, chunkSize / 2, LOD/2, heightMapMultiplier, parent, distanceThreshold / 2f);
    }
    public void InsertPoint(Vector3 viewerPosition)
    {
        /*If the viewer is not in the bounds of this region OR the chunkSize is too small then don't subdivide*/
        if (!bounds.Contains(viewerPosition) || chunkSize < minChunkSize) 
        {
            return;
        }
        Vector3 BLcentre = new Vector3(centre.x - chunkSize / 2, 0f, centre.z - chunkSize / 2); //
        Vector3 BRcentre = new Vector3(centre.x + chunkSize / 2, 0f, centre.z - chunkSize / 2);
        Vector3 TLcentre = new Vector3(centre.x - chunkSize / 2, 0f, centre.z + chunkSize / 2);
        Vector3 TRcentre = new Vector3(centre.x + chunkSize / 2, 0f, centre.z + chunkSize / 2);
        Vector3[] children = new Vector3[4]{ BLcentre, BRcentre, TLcentre, TRcentre};
        foreach (Vector3 point in children) 
        {
            Debug.Log(point);
            if (Vector3.Distance(viewerPosition, point) < distanceThreshold) 
            {
                Subdivide(BLcentre, BRcentre, TLcentre, TRcentre);
                divided = true; 
            }
        }
        if (divided) //If we've split this region, then insert the point into the subregions
        {
            BL.InsertPoint(viewerPosition);
            BR.InsertPoint(viewerPosition);
            TL.InsertPoint(viewerPosition);
            TR.InsertPoint(viewerPosition);
        }
        
    }
   
}
