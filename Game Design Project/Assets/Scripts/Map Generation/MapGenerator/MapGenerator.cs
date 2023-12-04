using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

public enum TopographyMode { FLAT, RELIEF }
public enum DrawNoiseMode { HEIGHT, LEVEL, HEAT, MOISTURE, BIOME };

public class MapGenerator : MonoBehaviour {

    [SerializeField] private TopographyMode topographyMode;
    [SerializeField] private DrawNoiseMode drawNoiseMode;
    [SerializeField] private bool autoUpdate;

    [Header("Map Settings")]
    [SerializeField, ReadOnly] private int mapChunkSize;
    [SerializeField][Range(0, 10)] public int editorDistanceViewChunks;
    [SerializeField] private float oceanNoiseLevel;
    [SerializeField] private float mountainNoiseLevel;
    [SerializeField] private float oceanMaxHeight;
    [SerializeField] private float plainMaxHeight;
    [SerializeField] private float mountainMaxHeight;
    [SerializeField] private AnimationCurve _oceanSmoothestCurveHeight;
    [SerializeField] private AnimationCurve _oceanCurveHeight;
    [SerializeField] private AnimationCurve _plainsCurveHeight;
    [SerializeField] private AnimationCurve _plainsSmoothestCurveHeight;
    [SerializeField] private AnimationCurve _moistureHeightCurve;

    [Header("Map Data")]
    [SerializeField] private NoiseData heightData;
    [SerializeField] private NoiseData oceansData;
    [SerializeField] private NoiseData plainsData;
    [SerializeField] private NoiseData mountainsData;
    [SerializeField] private NoiseData heatData;
    [SerializeField] private NoiseData moistureData;
    [SerializeField] private BiomeData biomeData;

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

            case DrawNoiseMode.LEVEL:
                DrawLevelMap(terrain, mapData);
                break;

            case DrawNoiseMode.HEAT:
                DrawHeatMap(terrain, mapData);
                break;

            case DrawNoiseMode.MOISTURE:
                DrawMoistureMap(terrain, mapData);
                break;

            case DrawNoiseMode.BIOME:
                DrawBiomeMap(terrain, mapData);
                break;
        }
    }

    private void DrawHeightMap(Terrain terrain, MapData mapData) {
        float[,,] splatsMap = MapGenerateUtils.GenerateSplatsMapForHeight(
            terrain.terrainData,
            mapData.heightMap,
            terrainBaseTextureResolution,
            terrainSmoothingEdge
        );

        terrain.terrainData.SetAlphamaps(0, 0, splatsMap);
    }

    private void DrawHeatMap (Terrain terrain, MapData mapData) {
        float[,,] splatsMap = MapGenerateUtils.GenerateSplatsMapForHeight(
            terrain.terrainData,
            mapData.heatMap,
            terrainBaseTextureResolution,
            terrainSmoothingEdge
        );

        terrain.terrainData.SetAlphamaps(0, 0, splatsMap);
    }

    private void DrawMoistureMap (Terrain terrain, MapData mapData) {
        float[,,] splatsMap = MapGenerateUtils.GenerateSplatsMapForHeight(
            terrain.terrainData,
            mapData.moistureMap,
            terrainBaseTextureResolution,
            terrainSmoothingEdge
        );

        terrain.terrainData.SetAlphamaps(0, 0, splatsMap);
    }

    private void DrawLevelMap(Terrain terrain, MapData mapData) {
        int[,] levelMap = new int[mapData.heightMap.GetLength(0), mapData.heightMap.GetLength(1)];
        for (int y = 0; y < mapData.heightMap.GetLength(0); y++) {
            for (int x = 0; x < mapData.heightMap.GetLength(1); x++) {
                float height = mapData.heightMap[y, x];
                if (height < oceanMaxHeight) {
                    levelMap[y, x] = 0;
                } else if (height < oceanMaxHeight + plainMaxHeight) {
                    levelMap[y, x] = 1;
                } else {
                    levelMap[y, x] = 2;
                }
            }
        }

        float[,,] splatsMap = MapGenerateUtils.GenerateSplatsMapForControl(
            terrain.terrainData,
            levelMap,
            terrainBaseTextureResolution
        );

        terrain.terrainData.SetAlphamaps(0, 0, splatsMap);
    }

    private void DrawBiomeMap (Terrain terrain, MapData mapData) {
        float[,,] splatsMap = MapGenerateUtils.GenerateSplatsMapForControl(
            terrain.terrainData,
            mapData.biomesMap,
            terrainBaseTextureResolution
        );

        terrain.terrainData.SetAlphamaps(0, 0, splatsMap);
    }

    private void DrawMesh(Terrain terrain, MapData mapData) {
        terrain.terrainData.SetHeights(0, 0, mapData.heightMap);
    }

    // ----------------- MAP GENERATION -----------------

    public MapData GenerateMapData(Vector2 centre) {
        centre = new Vector2(centre.y * (mapChunkSize - 1), -centre.x * (mapChunkSize - 1));

        float[,] heightMap = GenerateNoiseMap(centre);
        float[,] heatMap = MapGenerateUtils.CreateHeatNoise(mapChunkSize, centre, heatData, heightMap);
        float[,] moistureMap = MapGenerateUtils.CreateMoistureNoise(mapChunkSize, centre, moistureData, heightMap, heatMap, _moistureHeightCurve);
        int[,] biomesMap = MapGenerateUtils.CreateBiomesNoise(mapChunkSize, heightMap, heatMap, moistureMap, biomeData);

        return new MapData(heightMap, heatMap, moistureMap, biomesMap, null);
    }

    float[,] GenerateNoiseMap(Vector2 centre) {
        float[,] heightMap = new float[mapChunkSize, mapChunkSize];
        float[,] heightLevelMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, centre, heightData);

        float[,] oceansMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, centre, oceansData);
        float[,] plainsMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, centre, plainsData);
        float[,] mountainsMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, centre, mountainsData);

        AnimationCurve oceanSmoothestCurveHeight = new AnimationCurve(_oceanSmoothestCurveHeight.keys);
        AnimationCurve oceanCurveHeight = new AnimationCurve(_oceanCurveHeight.keys);
        AnimationCurve plainsCurveHeight = new AnimationCurve(_plainsCurveHeight.keys);
        AnimationCurve plainsSmoothestCurveHeight = new AnimationCurve(_plainsSmoothestCurveHeight.keys);

        int height = heightLevelMap.GetLength(0);
        int width = heightLevelMap.GetLength(1);

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float heightValue = heightLevelMap[y, x];
                if (heightValue < oceanNoiseLevel) {
                    float stepToPlain = (heightValue / oceanNoiseLevel);
                    float expectedHeight = oceanCurveHeight.Evaluate(stepToPlain) * oceanMaxHeight;
                    float actualHeight = oceansMap[y, x] * oceanMaxHeight;
                    float smoothest = oceanSmoothestCurveHeight.Evaluate(stepToPlain);

                    heightMap[y, x] = smoothest * expectedHeight + (1f - smoothest) * actualHeight;
                } else if (heightValue < mountainNoiseLevel) {
                    float stepToMountain = (heightValue - oceanNoiseLevel) / (mountainNoiseLevel - oceanNoiseLevel);
                    float expectedHeight = plainsCurveHeight.Evaluate(stepToMountain) * plainMaxHeight;
                    float actualHeight = plainsMap[y, x] * plainMaxHeight;
                    float smoothest = plainsSmoothestCurveHeight.Evaluate(stepToMountain);

                    heightMap[y, x] = oceanMaxHeight + smoothest * expectedHeight + (1f - smoothest) * actualHeight;
                } else {
                    float stepToMountain = (heightValue - mountainNoiseLevel) / (1 - mountainNoiseLevel);
                    float actualHeight = mountainsMap[y, x] * mountainMaxHeight;

                    heightMap[y, x] = oceanMaxHeight + plainMaxHeight + stepToMountain * actualHeight;
                }
            }
        }

        return heightMap;
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
        DefaultDataBiomes();
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

    private void DefaultDataBiomes() {
        for (int idx = 0; idx < terrainLayersGroup.Count; idx++) {
            TerrainLayersGroup terrainLayers = terrainLayersGroup[idx];
            if (terrainLayers.drawNoiseMode == DrawNoiseMode.BIOME) {
                terrainLayers.layers = new List<TerrainLayer>();
                foreach (BiomeData.Biome biome in biomeData.biomes) {
                    terrainLayers.layers.Add(biome.layer);
                }
                terrainLayersGroup[idx] = terrainLayers;
                break;
            }
        }
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