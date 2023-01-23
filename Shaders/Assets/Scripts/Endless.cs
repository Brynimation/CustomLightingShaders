using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Endless : MonoBehaviour
{
    [Range(0, 6)]
    [SerializeField] int LODTest;
    [SerializeField] float noiseScale;
    [SerializeField] float terrainHeightMultiplier = 10f;
    [SerializeField]Material terrainMaterial;
    public float renderDistance; // Number of units that the player can see - determines the number of surrounding chunks spawned.
    public Transform viewer;
    [SerializeField] bool visualiseChunks;
    [SerializeField] Material material;
    Vector3 viewerPosition;
    public int chunkSize;
    int chunksVisibleInViewDist;
    Dictionary<Vector2Int, TerrainChunk> terrainChunkDict;
    List<TerrainChunk> terrainChunksVisibleLastUpdate;
    void Start()
    {
        chunksVisibleInViewDist = Mathf.RoundToInt(renderDistance / chunkSize);
        terrainChunkDict = new Dictionary<Vector2Int, TerrainChunk>();
        terrainChunksVisibleLastUpdate = new List<TerrainChunk>();
    }

    // Update is called once per frame
    void Update()
    {
        viewerPosition = viewer.position;
        UpdateVisibleChunks();
    }
    void UpdateVisibleChunks() 
    {
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordZ = Mathf.RoundToInt(viewerPosition.z / chunkSize);
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
          
        }
        terrainChunksVisibleLastUpdate.Clear();

        /*Loop through all the chunks that should be rendered each frame*/
        for (int xOffset = -chunksVisibleInViewDist / 2; xOffset <= chunksVisibleInViewDist / 2; xOffset++) 
        {
            for (int zOffset = -chunksVisibleInViewDist / 2; zOffset <= chunksVisibleInViewDist / 2; zOffset++) 
            {
                Vector2Int viewedChunkCoord = new Vector2Int(
                    currentChunkCoordX + xOffset,
                    currentChunkCoordZ + zOffset);
                /*If we've already seen this chunk, then update it and add it to the list of chunks that were visible last frame*/
                if (terrainChunkDict.ContainsKey(viewedChunkCoord))
                {
                    TerrainChunk curChunk = terrainChunkDict[viewedChunkCoord];
                    curChunk.UpdateChunk(viewerPosition, renderDistance);
                    if (curChunk.isVisible())
                    {
                        terrainChunksVisibleLastUpdate.Add(curChunk);
                    }

                }
                /*If we haven't already seen a chunk at the given coord we instantiate a new one.*/
                else {
                    TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, chunkSize, terrainMaterial, this.transform, noiseScale, terrainHeightMultiplier, LODTest);
                    terrainChunkDict.Add(viewedChunkCoord, newChunk);
                    terrainChunksVisibleLastUpdate.Add(newChunk);
                }
            }
           
        }
    }
}

public class TerrainChunk 
{
    public Vector2Int coord;
    public Vector3 position;
    public int currentLevelOfDetail;
    GameObject terrainMesh;
    Bounds bounds;
    bool chunkVisible;
    void SpawnChunk(Vector3 position, int chunkSize, Material mat, Transform parent, float noiseScale, float terrainHeightMultiplier) 
    {
        terrainMesh = new GameObject("Mesh");
        MeshFilter mf = terrainMesh.AddComponent<MeshFilter>();
        MeshRenderer mr = terrainMesh.AddComponent<MeshRenderer>();
        terrainMesh.transform.parent = parent;
        terrainMesh.transform.localScale = Vector3.one * chunkSize / 10f; //A plane in unity is 10 units across by default
        //We need to access the renderer of our terrain mesh so we can apply the noise map to a texture
        float[][] noiseMap = Noise.GenerateNoiseMap(chunkSize, noiseScale);
        Renderer r = terrainMesh.GetComponent<Renderer>();
        r.material = mat;
        MeshGenerator.DrawAndApplyNoiseMap(noiseMap, ref r);
        MeshData meshData = MeshGenerator.GenerateMesh(noiseMap, terrainHeightMultiplier, currentLevelOfDetail);
        mf.mesh = meshData.mesh;
        terrainMesh.transform.position = position;
    }
    public TerrainChunk(Vector2Int coord, int chunkSize, Material mat, Transform parent, float noiseScale, float terrainHeightMultiplier, int currentLevelOfDetail) 
    {
        this.coord = coord;
        position = new Vector3(coord.x*chunkSize, 0f, coord.y * chunkSize);
        bounds = new Bounds(position, new Vector3(chunkSize, 1f, chunkSize));
        this.currentLevelOfDetail = currentLevelOfDetail;
        SpawnChunk(position, chunkSize, mat, parent, noiseScale, terrainHeightMultiplier);
    }

    /*Find the point on the perimeter of the chunk that is closest to the viewer's position and find the
     distance between that point and the viewer. If this is less than the render distanc, enable the mesh.
    Otherwise disable*/
    public void UpdateChunk(Vector3 viewerPosition, float renderDistance) 
    {
        float closestDstSqrd = bounds.SqrDistance(viewerPosition);
        chunkVisible = closestDstSqrd <= renderDistance * renderDistance;
        SetVisible(chunkVisible);
    }
    public bool isVisible() 
    {
        return true;
    }
    public void SetVisible(bool visible) 
    {
        terrainMesh.SetActive(visible);
    }
}
