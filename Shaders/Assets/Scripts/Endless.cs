using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Endless : MonoBehaviour
{
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
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordZ = Mathf.RoundToInt(viewerPosition.z / chunkSize);

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
                    TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, chunkSize, terrainMaterial, this.transform, noiseScale, terrainHeightMultiplier);
                    terrainChunkDict.Add(viewedChunkCoord, newChunk);
                }
            }
           
        }
    }
}

public class TerrainChunk 
{
    Vector3 position;
    GameObject terrainMesh;
    Bounds bounds;
    bool chunkVisible;
    void DrawNoiseMap(float[][] noiseMap, Renderer r) 
    {
        int width = noiseMap.Length;
        int height = noiseMap[0].Length;
        Color[] pixels = new Color[width * height];
        Texture2D noiseTexture = new Texture2D(width, height);
        r.material.mainTexture = noiseTexture;
        //r.material.color = Color.red;
        Debug.Log(string.Format("{0},{1}", width, height));
        for (int y = 0; y < height; y++) 
        {
            for(int x = 0; x < width; x++) 
            {
                pixels[(int)(y * width) + (int)x] = Color.Lerp(Color.black, Color.white, noiseMap[x][y]);
            }
        }
        noiseTexture.SetPixels(pixels);
        noiseTexture.Apply();
        r.transform.localScale = new Vector3(width, 1, height);
    }
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
        DrawNoiseMap(noiseMap, r);
        MeshData meshData = MeshGenerator.GenerateMesh(noiseMap, terrainHeightMultiplier);
        mf.mesh = meshData.mesh;
        terrainMesh.transform.position = position;
    }
    public TerrainChunk(Vector2Int coord, int chunkSize, Material mat, Transform parent, float noiseScale, float terrainHeightMultiplier) 
    {
        position = new Vector3(coord.x*chunkSize * 10, 0f, coord.y * chunkSize * 10);
        bounds = new Bounds(position, new Vector3(chunkSize, 1f, chunkSize));
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
        if(chunkVisible) terrainMesh.SetActive(visible);
    }
}
