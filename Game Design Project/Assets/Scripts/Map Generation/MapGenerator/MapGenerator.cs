using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

public enum TopographyMode { FLAT, RELIEF }
public enum DrawNoiseMode { HEIGHT };

public class MapGenerator : MonoBehaviour {

    [SerializeField] private TopographyMode topographyMode;
    [SerializeField] private DrawNoiseMode drawNoiseMode;
    [SerializeField] private bool autoUpdate;

    [Header("Map Settings")]
    [SerializeField, ReadOnly] private int mapChunkSize;
    [SerializeField][Range(0, 10)] public int editorDistanceViewChunks;

    [Header("Map Data")]
    [SerializeField] private NoiseData heightData;

    [Header("Terrain")]
    [SerializeField] private GameObject terrainPrefab;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private Material terrainMaterialForTexture;
    [SerializeField] private List <TerrainLayersGroup> terrainLayersGroup = new List<TerrainLayersGroup>();
    [SerializeField] private int terrainSmoothingEdge = 5;

    private int terrainChunkSize;
    private int terrainChunkHeight;
    private int terrainDetailResolutionPerPatch;
    private int terrainDetailResolution;
    private int terrainControlTextureResolution;
    private int terrainBaseTextureResolution;

    Queue<Tuple<Terrain, int, int>> queue = new Queue<Tuple<Terrain, int, int>>();
    Queue <TerrainDataThread> queueChunksThread = new Queue<TerrainDataThread>();
    Dictionary<Vector2Int, bool> terrainChunkWasGenerated = new Dictionary<Vector2Int, bool>();

    public void Initialize() {
        ClearData();
        DefaultData();
    }

    void Update() {
        while (queueChunksThread.Count > 0) {
            TerrainDataThread terrainDataThread;
            lock (queueChunksThread) {
                terrainDataThread = queueChunksThread.Dequeue();
            }
            terrainDataThread.callback(terrainDataThread.mapData);
        }
    }

    // ----------------- DRAW MODES -----------------

    public void DrawMapInEditor() {
        ClearData();
        DefaultData();
        DrawVisibleChunks();
    }

    private void DrawVisibleChunks() {
        Terrain terrain = GenerateNewTerrainChunk();
        SetDefaultSettingsTerrainChunk(terrain, null, Vector2Int.zero, -1);

        queue.Enqueue(new Tuple<Terrain, int, int>(terrain, 0, 0));
        terrainChunkWasGenerated.Add(Vector2Int.zero, true);
        while (queue.Count > 0) {
            Terrain parentTerrain = queue.Peek().Item1;
            Vector2Int position = new Vector2Int(queue.Peek().Item2, queue.Peek().Item3);

            MapData mapData = GenerateMapData(new Vector2(position.x, position.y));
            DrawTerrain(parentTerrain, mapData);

            queue.Dequeue();

            for (int dir = 0; dir < 4; dir++) {
                Vector2Int offset = position + Direction.GetOffset(dir);
                int distance = Mathf.Abs(offset.x) + Mathf.Abs(offset.y);
                if (distance <= editorDistanceViewChunks && !terrainChunkWasGenerated.ContainsKey(offset)) {
                    terrainChunkWasGenerated.Add(offset, true);
                    terrain = GenerateNewTerrainChunk();
                    SetDefaultSettingsTerrainChunk(terrain, parentTerrain, offset, dir);
                    queue.Enqueue(new Tuple<Terrain, int, int>(terrain, offset.x, offset.y));
                }
            }
        }
    }

    public void DrawTerrain(Terrain terrain, MapData mapData) {
        switch (topographyMode) {
            case TopographyMode.RELIEF:
                DrawMesh(terrain, mapData);
                break;
            case TopographyMode.FLAT:
                break;
        }

        switch (drawNoiseMode) {
            case DrawNoiseMode.HEIGHT:
                DrawHeightMap(terrain, mapData);
                break;
        }
    }

    private void DrawHeightMap(Terrain terrain, MapData mapData) {
        terrain.terrainData.SetAlphamaps(0, 0, GenerateSplatsMap(terrain.terrainData, mapData.heightMap));
    }

    private void DrawMesh(Terrain terrain, MapData mapData) {
        terrain.terrainData.SetHeights(0, 0, TransformMatrixIntoMinusTwo(mapData.heightMap));
    }

    // ----------------- MAP GENERATION -----------------

    public MapData GenerateMapData(Vector2 centre) {
        centre = new Vector2(centre.y * (mapChunkSize - 1), -centre.x * (mapChunkSize - 1));

        float[,] baseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, centre, heightData);

        return new MapData(baseMap, null, null, null, null);
    }

    public Terrain GenerateNewTerrainChunk() {
        return Instantiate(terrainPrefab, transform).GetComponent<Terrain>();
    }

    public void SetDefaultSettingsTerrainChunk(Terrain terrain, Terrain parentTerrain, Vector2Int offset, int direction) {
        terrain.gameObject.name = "Terrain(" + offset.x + "," + offset.y + ")";
        terrain.transform.parent = transform;
        terrain.transform.position = new Vector3(offset.x * terrainChunkSize, 0, offset.y * terrainChunkSize);

        terrain.terrainData = new TerrainData();
        terrain.terrainData.heightmapResolution = mapChunkSize;
        terrain.terrainData.size = new Vector3(terrainChunkSize, terrainChunkHeight, terrainChunkSize);
        terrain.terrainData.SetDetailResolution(terrainDetailResolution, terrainDetailResolutionPerPatch);
        terrain.terrainData.alphamapResolution = terrainControlTextureResolution;
        terrain.terrainData.baseMapResolution = terrainBaseTextureResolution;
        foreach (TerrainLayersGroup terrainLayers in terrainLayersGroup) {
            if (terrainLayers.drawNoiseMode == drawNoiseMode) {
                terrain.terrainData.terrainLayers = terrainLayers.layers.ToArray();
                break;
            }
        }

        terrain.GetComponent<TerrainCollider>().terrainData = terrain.terrainData;

        terrain.materialTemplate = new Material(terrainMaterial);

        if (parentTerrain != null) {
            if (direction == Direction.NORTH) {
                parentTerrain.SetNeighbors(null, terrain, null, null);
            } else if (direction == Direction.EAST) {
                parentTerrain.SetNeighbors(null, null, terrain, null);
            } else if (direction == Direction.SOUTH) {
                parentTerrain.SetNeighbors(null, null, null, terrain);
            } else if (direction == Direction.WEST) {
                parentTerrain.SetNeighbors(terrain, null, null, null);
            }
        }
    }

    private float[,,] GenerateSplatsMap(TerrainData terrainData, float[,] heightMap) {
        int layerCount = terrainData.terrainLayers.Length;
        float[,,] splatmapData = new float[terrainBaseTextureResolution, terrainBaseTextureResolution, layerCount];

        for (int y = 1; y < terrainBaseTextureResolution + 1; y++) {
            for (int x = 1; x < terrainBaseTextureResolution + 1; x++) {
                int numDivisions = 0;
                float height = 0;

                if (x == 0) {
                    height = heightMap[y, x - 1];
                    ++numDivisions;
                } else if (x >= terrainBaseTextureResolution - terrainSmoothingEdge) {
                    int distance = terrainBaseTextureResolution - x;
                    height = heightMap[y, x + 1] * (1 - 1f * distance / terrainSmoothingEdge) + heightMap[y, x] * (1f * distance / terrainSmoothingEdge);
                    ++numDivisions;
                }

                if (y == 0) {
                    height += heightMap[y - 1, x];
                } else if (y >= terrainBaseTextureResolution - terrainSmoothingEdge) {
                    int distance = terrainBaseTextureResolution - y;
                    height += heightMap[y + 1, x] * (1 - 1f * distance / terrainSmoothingEdge) + heightMap[y, x] * (1f * distance / terrainSmoothingEdge);
                    ++numDivisions;
                }

                if (numDivisions > 0)
                    height /= numDivisions;
                else height = heightMap[y, x];

                splatmapData[y - 1, x - 1, 0] = 1 - height;
                splatmapData[y - 1, x - 1, 1] = height;
            }
        }

        return splatmapData;
    }

    // ----------------- THREADS -----------------

    public void RequestChunk(Terrain terrain, Terrain parentTerrain, Vector2Int positionChunk, int dir, Action<MapData> callback) {
        ThreadStart threadStart = delegate {
            GenerateChunkThread(positionChunk, callback);
        };

        new Thread(threadStart).Start();
    }

    private void GenerateChunkThread(Vector2Int positionChunk, Action<MapData> callback) {
        MapData mapData = GenerateMapData(new Vector2(positionChunk.x, positionChunk.y));

        lock (queueChunksThread) {
            queueChunksThread.Enqueue(new TerrainDataThread(mapData, callback));
        }
    }

    // ----------------- TRANSFORM -----------------

    private float[,] TransformMatrixIntoMinusTwo(float[,] matrix) {
        float[,] newMatrix = new float[matrix.GetLength(0) - 2, matrix.GetLength(1) - 2];
        for (int y = 1; y < newMatrix.GetLength(0) + 1; y++) {
            for (int x = 1; x < newMatrix.GetLength(1) + 1; x++) {
                newMatrix[y - 1, x - 1] = matrix[y + 1, x + 1];
            }
        }
        return newMatrix;
    }

    // ----------------- GETTERS AND SETTERS -----------------

    public DrawNoiseMode GetDrawMode() {
        return drawNoiseMode;
    }

    public bool GetAutoUpdate() {
        return autoUpdate;
    }

    public int GetMapChunkSize() {
        return mapChunkSize;
    }

    // ----------------- DEFAULT DATA -----------------

    public void DefaultData() {
        DefaultDataTerrain();
    }

    private void DefaultDataTerrain() {
        Terrain terrain = terrainPrefab.GetComponent<Terrain>();

        Debug.Assert((int)terrain.terrainData.size.x == (int)terrain.terrainData.size.z, "Width and length aren't the same!");

        queue.Clear();
        terrainChunkWasGenerated.Clear();

        queue = new Queue<Tuple<Terrain, int, int>>();
        terrainChunkWasGenerated = new Dictionary<Vector2Int, bool>();

        terrainChunkSize = (int)terrain.terrainData.size.x;
        terrainChunkHeight = (int)terrain.terrainData.size.y;
        mapChunkSize = terrain.terrainData.heightmapResolution;
        terrainDetailResolutionPerPatch = terrain.terrainData.detailResolutionPerPatch;
        terrainDetailResolution = terrain.terrainData.detailResolution;
        terrainControlTextureResolution = terrain.terrainData.alphamapWidth;
        terrainBaseTextureResolution = terrain.terrainData.baseMapResolution;
    }

    // ----------------- CLEAR DATA -----------------

    public void ClearData() {
        DeleteExitingTerrain();
    }

    private void DeleteExitingTerrain() {
        while (transform.childCount > 0) {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

}

public struct MapData {
    public readonly float[,] heightMap;
    public readonly float[,] heatMap;
    public readonly float[,] moistureMap;
    public readonly int[,] biomesMap;

    public readonly float[,] treesMap;

    public MapData(float[,] heightMap, float[,] heatMap, float[,] moistureMap, int[,] biomesMap, float[,] treesMap) {
        this.heightMap = heightMap;
        this.heatMap = heatMap;
        this.moistureMap = moistureMap;
        this.biomesMap = biomesMap;
        this.treesMap = treesMap;
    }
}

[Serializable]
public struct TerrainLayersGroup {
    public string _name;
    public List <TerrainLayer> layers;
    public DrawNoiseMode drawNoiseMode;
}

[Serializable]
public struct TerrainDataThread {
    public MapData mapData;
    public Action<MapData> callback;

    public TerrainDataThread(MapData mapData, Action<MapData> callback) {
        this.mapData = mapData;
        this.callback = callback;
    }
}