using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuadTreeTest : MonoBehaviour
{
    [SerializeField] Transform viewerTransform;
    [SerializeField] int chunkSize;
    [SerializeField] float heightMapMultiplier;
    void Start()
    {
        TerrainChunkQuadTree root = new TerrainChunkQuadTree(Vector3.zero, chunkSize, 8,heightMapMultiplier, this.transform, chunkSize * 0.75f);
        root.InsertPoint(viewerTransform.position);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
