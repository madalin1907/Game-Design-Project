using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;
using static MapGenerator;

public enum Resources { TEMEPERATE_FOREST };

public enum TopographyMode { FLAT, RELIEF }
public enum DrawNoiseMode { HEIGHT, LEVEL, HEAT, MOISTURE, BIOME, RESOURCES };

public class MapGenerator : MonoBehaviour {

    [SerializeField] private TopographyMode topographyMode;
    [SerializeField] private DrawNoiseMode drawNoiseMode;
    [SerializeField] private bool drawTrees;

    [SerializeField] private bool autoUpdate;

    [Header("Map Settings")]
    [SerializeField, ReadOnly] private int mapChunkSize;
    [SerializeField][Range(0, 10)] public int editorDistanceViewChunks;
    [SerializeField] private float oceanNoiseLevel;
    [SerializeField] private float mountainNoiseLevel;
    [SerializeField] private float oceanMaxHeight;
    [SerializeField] private float plainMaxHeight;
    [SerializeField] private float mountainMaxHeight;
    [SerializeField] private float densityResources;
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
    [SerializeField] private NoiseData resourcesData;
    [SerializeField] private BiomeData biomeData;

    [Header("Terrain")]
    [SerializeField] private GameObject terrainPrefab;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private Material terrainMaterialForTexture;
    [SerializeField] private List <TerrainLayersGroup> terrainLayersGroup = new List<TerrainLayersGroup>();
    [SerializeField] private int terrainSmoothingEdge = 5;

    [Header("Resources")]

    [SerializeField] private List <ResourcesData> resources = new List<ResourcesData>();
    Dictionary<BiomeTag, bool> biomesApparition = new Dictionary<BiomeTag, bool>();

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

            case DrawNoiseMode.RESOURCES:
                DrawResourcesMap(terrain, mapData);
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

    private void DrawResourcesMap (Terrain terrain, MapData mapData) {
        float[,] floatResourcesMap = new float[mapData.resourcesMap.GetLength(0), mapData.resourcesMap.GetLength(1)];
        for (int y = 0; y < mapData.resourcesMap.GetLength(0); y++) {
            for (int x = 0; x < mapData.resourcesMap.GetLength(1); x++) {
                if (mapData.resourcesMap[y, x] >= 0)
                    floatResourcesMap[y, x] = 1;
            }
        }

        float[,,] splatsMap = MapGenerateUtils.GenerateSplatsMapForHeight(
            terrain.terrainData,
            floatResourcesMap,
            terrainBaseTextureResolution,
            0
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

        if (drawTrees)
            DrawTreesMap(terrain, mapData);
    }

    private void DrawTreesMap(Terrain terrain, MapData mapData) {
        GameObject chunkObject = terrain.gameObject;

        GameObject treesParentObject = new GameObject("Trees");
        treesParentObject.transform.parent = chunkObject.transform;
        treesParentObject.transform.localPosition = Vector3.zero;

        for (int y = 0; y < mapData.heightMap.GetLength(0); y++) {
            for (int x = 0; x < mapData.heightMap.GetLength(1); x++) {
                float height = mapData.heightMap[y, x];
                if (height < oceanMaxHeight || height > oceanMaxHeight + plainMaxHeight || mapData.resourcesMap[y, x] == -1)
                    continue;

                GenerateResource(treesParentObject.transform, new Vector3(x, height, y), mapData.resourcesMap[y, x]);
                
                mapData.resourcesMap[y, x] = -1;
            }
        }
    }

    // ----------------- MAP GENERATION -----------------

    public MapData GenerateMapData(Vector2 centre) {
        centre = new Vector2(centre.y * (mapChunkSize - 1), -centre.x * (mapChunkSize - 1));

        float[,] heightMap = GenerateNoiseMap(centre);

        float[,] heatMap = MapGenerateUtils.CreateHeatNoise(mapChunkSize, centre, heatData, heightMap);
        float[,] moistureMap = MapGenerateUtils.CreateMoistureNoise(mapChunkSize, centre, moistureData, heightMap, heatMap, _moistureHeightCurve);
        int[,] biomesMap = MapGenerateUtils.CreateBiomesNoise(mapChunkSize, heightMap, heatMap, moistureMap, biomeData);

        int[,] resourcesMap = CreateResourcesNoiseMap(centre, densityResources, heightMap, biomesMap);

        return new MapData(heightMap, heatMap, moistureMap, biomesMap, resourcesMap);
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

    int[,] CreateResourcesNoiseMap(Vector2 centre, float rarity, float[,] heightMap, int[,] biomesMap) {
        float[,] baseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, centre, resourcesData);

        int lengthR = baseMap.GetLength(0);
        int lengthC = baseMap.GetLength(1);
        int marginAround = 3;

        for (int x = 0; x < lengthR; x++) {
            for (int y = 0; y < lengthC; y++) {

                if (x < marginAround || y < marginAround) {
                    baseMap[x, y] = 0;
                    continue;
                }

                if (baseMap[x, y] < rarity) {
                    baseMap[x, y] = 0;
                    continue;
                }

                baseMap[x, y] = 1;

                for (int i = x - marginAround; i <= x + marginAround; i++) {
                    if (i < 0 || i >= lengthR)
                        continue;
                    for (int j = y - marginAround; j <= y + marginAround; j++) {
                        if (j < 0 || j >= lengthC)
                            continue;
                        if (baseMap[i, j] == 1)
                            continue;
                        baseMap[i, j] = 0;
                    }
                }
            }
        }

        return CreateResourcesTypeNoiseMap(baseMap, heightMap, biomesMap);
    }

    private int[,] CreateResourcesTypeNoiseMap(float[,] baseMap, float[,] heightMap, int[,] biomesMap) {
        int[,] resourcesMap = new int[baseMap.GetLength(0), baseMap.GetLength(1)];

        int lengthR = baseMap.GetLength(0);
        int lengthC = baseMap.GetLength(1);

        for (int y = 0; y < lengthR; y++) {
            for (int x = 0; x < lengthC; x++) {
                resourcesMap[y, x] = -1;
                if (baseMap[y, x] == 0)
                    continue;

                BiomeTag tag = MapGenerateUtils.GetBiomeTag(biomesMap[y, x]);
                if (!biomesApparition.ContainsKey(tag)) {
                    continue;
                }

                float totalProbability = 0;
                for (int i = 0; i < resources.Count; i++) {
                    if (resources[i].biome != tag)
                        continue;
                    totalProbability += resources[i].probability;
                }

                float randomValue = (MapGenerateUtils.intPseudoRandom2(x, y) % 1000 / 1000f) * Mathf.Max(1, totalProbability);
                float currentProbability = 0;
                for (int i = 0; i < resources.Count; i++) {
                    if (resources[i].biome != tag)
                        continue;
                    if (currentProbability <= randomValue && randomValue <= currentProbability + resources[i].probability) {
                        resourcesMap[y, x] = i;
                        break;
                    }
                    currentProbability += resources[i].probability;
                }
            }
        }

        return resourcesMap;
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

    // ----------------- Resources -----------------

    private void GenerateResource(Transform parentTransform, Vector3 relativePos, int indexResource) {
        if (parentTransform == null)
            return;

        float yOffset = -0.3f;
        float yRotation;
        float adaptiveScale;
        Vector3 position;
        GameObject resource;

        yRotation = MapGenerateUtils.intPseudoRandom2((int)relativePos.x, (int)relativePos.z) % 360;
        adaptiveScale = (MapGenerateUtils.intPseudoRandom2((int)relativePos.x, (int)relativePos.z) % 5 - 2) / 10f;
        position = parentTransform.position + new Vector3(2 * relativePos.x, 64 * relativePos.y + yOffset, 2 * relativePos.z);
        
        resource = Instantiate(resources[indexResource].gameObject, position, Quaternion.identity, parentTransform);

        resource.transform.localScale = Vector3.one;
        resource.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        resource.transform.localScale = Vector3.one + adaptiveScale * Vector3.one;
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
        DefaultDataResources();
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
        MapGenerateUtils.InitializeBiomesTagIndex(biomeData);
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

    private void DefaultDataResources() {
        for (int i = 0; i < resources.Count; i++) {
            if (!biomesApparition.ContainsKey(resources[i].biome)) {
                biomesApparition.Add(resources[i].biome, true);
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

    public readonly int[,] resourcesMap;

    public MapData(float[,] heightMap, float[,] heatMap, float[,] moistureMap, int[,] biomesMap, int[,] resourcesMap) {
        this.heightMap = heightMap;
        this.heatMap = heatMap;
        this.moistureMap = moistureMap;
        this.biomesMap = biomesMap;
        this.resourcesMap = resourcesMap;
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

[Serializable]
public struct ResourcesData {
    public float probability;
    public BiomeTag biome;
    public GameObject gameObject;
}