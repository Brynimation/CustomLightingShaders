using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TerrainChunkQuadTree 
{
    public Bounds bounds;
    public int minChunkSize;
    Vector3 centre;
    float chunkSize;
    bool divided;
    TerrainChunkQuadTree BL;
    TerrainChunkQuadTree BR;
    TerrainChunkQuadTree TL;
    TerrainChunkQuadTree TR;


    public TerrainChunkQuadTree(Vector3 centre, float chunkSize) 
    {
        bounds = new Bounds(centre, new Vector3(chunkSize, 1f, chunkSize));
    }

    public void Subdivide(Vector3 BLCentre, Vector3 BRCentre, Vector3 TLCentre, Vector3 TRCentre) 
    {
        BL = new TerrainChunkQuadTree(BLCentre, chunkSize / 2);
        BR = new TerrainChunkQuadTree(BRCentre, chunkSize / 2);
        TL = new TerrainChunkQuadTree(TLCentre, chunkSize / 2);
        TR = new TerrainChunkQuadTree(TRCentre, chunkSize / 2);
    }
    public void InsertPoint(Vector3 viewerPosition, float chunkSize, float distanceThreshold)
    {
        /*If the viewer is not in the bounds of this region OR the chunkSize is too small then don't subdivide*/
        if (!bounds.Contains(viewerPosition) || chunkSize < minChunkSize) 
        {
            return;
        }
        Vector3 BLcentre = new Vector3(centre.x - chunkSize / 2, 0f, centre.z - chunkSize / 2); //bottom left
        Vector3 BRcentre = new Vector3(centre.x + chunkSize / 2, 0f, centre.z - chunkSize / 2);
        Vector3 TLcentre = new Vector3(centre.x - chunkSize / 2, 0f, centre.z + chunkSize / 2);
        Vector3 TRcentre = new Vector3(centre.x + chunkSize / 2, 0f, centre.z + chunkSize / 2);
        Vector3[] children = new Vector3[4]{ BLcentre, BRcentre, TLcentre, TRcentre};
        foreach (Vector3 point in children) 
        {
            if (Vector3.Distance(viewerPosition, point) < distanceThreshold && !divided) 
            {
                Subdivide(BLcentre, BRcentre, TLcentre, TRcentre);
                divided = true; 
            }
        }
        if (divided) //If we've split this region, then insert the point into the subregions
        {
            BL.InsertPoint(viewerPosition, chunkSize / 2, chunkSize / 2);
            BR.InsertPoint(viewerPosition, chunkSize / 2, chunkSize / 2);
            TL.InsertPoint(viewerPosition, chunkSize / 2, chunkSize / 2);
            TR.InsertPoint(viewerPosition, chunkSize / 2, chunkSize / 2);
        }
        
    }
   
}
