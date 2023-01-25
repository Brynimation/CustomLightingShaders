using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using Unity.Mathematics;

public class Endless : MonoBehaviour
{
    //Singleton. We only ever need one instance of Endless.
    public static Endless Instance;
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
    Queue<ThreadData<NoiseMapData>> noiseMapQueue = new Queue<ThreadData<NoiseMapData>>();
    Queue<ThreadData<MeshData>> meshDataQueue = new Queue<ThreadData<MeshData>>();
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }
    void Start()
    {
        chunksVisibleInViewDist = Mathf.RoundToInt(renderDistance / chunkSize);
        terrainChunkDict = new Dictionary<Vector2Int, TerrainChunk>();
        terrainChunksVisibleLastUpdate = new List<TerrainChunk>();
    }
    public void RequestMapData(Action<NoiseMapData> callback) 
    {
        //Thread start delegate represents our MapDataThread.
        ThreadStart threadStart = delegate
        {
            MapDataThread(callback);
        };
        /*The method that we passed in as a delegate will now be running 
        on this new thread.*/
        new Thread(threadStart).Start();
    }
    public void MapDataThread(Action<NoiseMapData> callback) 
    {
        NoiseMapData noiseMapData = MeshGenerator.GenerateNoiseMapData(chunkSize, noiseScale);
        //Since we're running this method on a separate thread, we need to avoid 
        //race conditions by preventing multiple threads from accessing the queue at once.
        //We do this with the lock keyword
        lock(noiseMapQueue){
            noiseMapQueue.Enqueue(new ThreadData<NoiseMapData>(noiseMapData, callback));
        }
        
    }

    public void RequestMeshData(NoiseMapData noiseMapData, int LOD, Action<MeshData> callback)
    {
        
        ThreadStart threadStart = delegate
        {
            MeshDataThread(noiseMapData, LOD, callback);
        };
        /*The method that we passed in as a delegate will now be running 
        on a separate thread.*/
        new Thread(threadStart).Start();
    }
    public void MeshDataThread(NoiseMapData noiseMapData, int LOD, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateMesh(noiseMapData.noiseMap, terrainHeightMultiplier, LOD);
        //Since we're running this method on a separate thread, we need to avoid 
        //race conditions by preventing multiple threads from accessing the queue at once.
        //We do this with the lock keyword
        lock (meshDataQueue)
        {
            meshDataQueue.Enqueue(new ThreadData<MeshData>(meshData, callback));
        }

    }
    void Update()
    {
        if (noiseMapQueue.Count > 0) {
            for (int i = 0; i < noiseMapQueue.Count; i++) 
            {
                ThreadData<NoiseMapData> threadData = noiseMapQueue.Dequeue();
                threadData.callBack(threadData.data);
            }
        }

        if (meshDataQueue.Count > 0)
        {
            for (int i = 0; i < meshDataQueue.Count; i++)
            {
                ThreadData<MeshData> threadData = meshDataQueue.Dequeue();
                threadData.callBack(threadData.data);
            }
        }
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
    public Material mat;
    public Vector3 position;
    public int currentLevelOfDetail;
    public int chunkSize;
    public MeshFilter mf;
    public MeshRenderer r;
    GameObject terrainMesh;
    Bounds bounds;
    bool chunkVisible;
    void SpawnChunk(Vector3 position, int chunkSize, Material mat, Transform parent, float noiseScale, float terrainHeightMultiplier)
    {

        //Create new mesh object and parent it to this gameobject
        terrainMesh = new GameObject("Mesh");
        mf = terrainMesh.AddComponent<MeshFilter>();
        r = terrainMesh.AddComponent<MeshRenderer>();
        terrainMesh.transform.parent = parent;
        terrainMesh.transform.position = position;

        //Request MapData. The MapData generation will be performed on a separate thread.
        Endless.Instance.RequestMapData(OnMapDataReceived);
        //We access the renderer of our terrain mesh so we can apply the noise map to a texture

        //We now use the noise map data to generate the mesh.

    }

    void OnMapDataReceived(NoiseMapData noiseMapData)
    {
        //Once the noisemap is generated we can request the corresponding mesh data.
        //Again, the generation of the mesh is run on a separate thread.
        Endless.Instance.RequestMeshData(noiseMapData, currentLevelOfDetail, OnMeshDataReceived);
        //The actual creation and setting of the texture must be done on the main thread
        r.material = mat;
        Texture2D noiseTexture = new Texture2D(chunkSize, chunkSize);
        r.material.mainTexture = noiseTexture;
        noiseTexture.SetPixels(noiseMapData.colourMap);
        noiseTexture.Apply();
        //r.transform.localScale = new Vector3(chunkSize / 10f, 1, chunkSize / 10f);
    }
    void OnMeshDataReceived(MeshData meshData) 
    {
        //The actual creation and setting of the mesh must be done on the main thread.
        mf.mesh = meshData.getMesh();
        Debug.Log("Mesh Generated!");
    }
    public TerrainChunk(Vector2Int coord, int chunkSize, Material mat, Transform parent, float noiseScale, float terrainHeightMultiplier, int currentLevelOfDetail) 
    {
        /*Each TerrainChunk is a quad tree; a data structure where every node has four children. It is a means by
         which we can partition 2D space (ie, it allows us to partition a plane) into children with different 
        Levels Of Details.*/
        TerrainChunk[] children = new TerrainChunk[4];
        this.coord = coord;
        position = new Vector3(coord.x*chunkSize, 0f, coord.y * chunkSize);
        bounds = new Bounds(position, new Vector3(chunkSize, 1f, chunkSize));
        this.currentLevelOfDetail = currentLevelOfDetail;
        this.chunkSize = chunkSize;
        this.mat = mat;
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
